namespace Infrastructure;

/// <summary>
/// Worker nền bền dựng lại dữ liệu dẫn xuất từ hàng đợi <see cref="RecalculationJob"/> trong DB (không còn hàng đợi trong bộ nhớ):
/// - Nhận job bằng UPDATE có điều kiện (chỉ một instance thắng); job Running quá hạn lease (instance chết/restart giữa chừng) được nhận lại; job Failed thử lại với giãn cách tăng dần tới số lần tối đa.
/// - Mỗi lần chạy dưới khoá user (<c>sp_getapplock</c>) và đọc nguồn SAU khi giữ khoá, gộp mọi job Pending của user; xong đánh dấu Succeeded cùng transaction với snapshot (xem <see cref="DerivedDataJobs"/>).
/// - Đánh thức ngay khi instance này xếp job (<see cref="Wake"/>), ngoài ra quét theo chu kỳ để nhặt job của instance khác/sau restart.
/// </summary>
[RegisterService(typeof(IDerivedDataScheduler), ServiceLifetime.Singleton)]
public sealed class DerivedDataWorker(IServiceScopeFactory scopes, IOptions<AlgorithmOptions> options, TimeProvider time, ILogger<DerivedDataWorker> logger)
    : BackgroundService, IDerivedDataScheduler
{
    private readonly SemaphoreSlim _wake = new(0);

    public void Wake() => _wake.Release();

    public void ReportSync(string cause, long elapsedMs, int rowsWritten, string outcome)
        => logger.LogInformation("Derived data sync rebuild after {Cause}: {ElapsedMs} ms, {Rows} rows, {Outcome}", cause, elapsedMs, rowsWritten, outcome);

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        // Algorithm:BackgroundDerivedProcessingEnabled = false: không chạy vòng worker (API cục bộ dùng chung DB); RunPendingAsync vẫn gọi tay được
        if (!options.Value.BackgroundDerivedProcessingEnabled)
        {
            logger.LogInformation("Derived data worker disabled (Algorithm:BackgroundDerivedProcessingEnabled = false)");
            return;
        }

        while (!stop.IsCancellationRequested)
        {
            try { await RunPendingAsync(stop); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Derived data worker pass failed"); }

            try { await _wake.WaitAsync(TimeSpan.FromSeconds(Math.Max(1, options.Value.DerivedWorkerPollSeconds)), stop); }
            catch (OperationCanceledException) { return; }
            while (_wake.CurrentCount > 0) _wake.Wait(0); // gộp các lần đánh thức dồn
        }
    }

    /// <summary>Quét một lượt: xử lý mỗi user có job đến hạn một lần. Trả số user đã dựng xong. Public để test và công cụ dòng lệnh gọi tay.</summary>
    public async Task<int> RunPendingAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        var now = time.GetUtcNow();
        List<(Guid JobId, Guid UserId)> due;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var lease = now.AddSeconds(-opt.DerivedJobLeaseSeconds);
            var rows = await db.Set<RecalculationJob>().AsNoTracking()
                .Where(j => j.State == JobState.Pending
                    || (j.State == JobState.Running && j.StartedAt < lease)
                    || (j.State == JobState.Failed && j.AttemptCount < opt.DerivedJobMaxAttempts))
                .OrderBy(j => j.CreatedAt).Take(200)
                .Select(j => new { j.Id, j.UserId, j.State, j.AttemptCount, j.FinishedAt })
                .ToListAsync(ct);
            // Failed: chờ giãn cách cơ sở × 2^(lần thử − 1) kể từ lần thất bại gần nhất
            due = rows.Where(j => j.State != JobState.Failed || j.FinishedAt is null || j.FinishedAt.Value.AddSeconds(opt.DerivedJobRetryBaseSeconds * Math.Pow(2, Math.Max(0, j.AttemptCount - 1))) <= now)
                .GroupBy(j => j.UserId).Select(g => (g.First().Id, g.Key)).ToList();

            await db.Set<RecalculationJob>().Where(j => j.State == JobState.Succeeded && j.FinishedAt < now.AddDays(-7)).ExecuteDeleteAsync(ct); // dọn job cũ đã xong
        }

        var done = 0;
        foreach (var (jobId, userId) in due)
            if (await ProcessAsync(jobId, userId, ct)) done++;
        return done;
    }

    private async Task<bool> ProcessAsync(Guid jobId, Guid userId, CancellationToken ct)
    {
        var opt = options.Value;
        var now = time.GetUtcNow();
        var lease = now.AddSeconds(-opt.DerivedJobLeaseSeconds);
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Nhận job: chỉ một instance thắng (UPDATE có điều kiện). Running quá hạn lease = instance trước đã chết.
        var claimed = await db.Set<RecalculationJob>()
            .Where(j => j.Id == jobId && (j.State == JobState.Pending || (j.State == JobState.Running && j.StartedAt < lease) || (j.State == JobState.Failed && j.AttemptCount < opt.DerivedJobMaxAttempts)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.State, JobState.Running)
                .SetProperty(j => j.StartedAt, now)
                .SetProperty(j => j.FinishedAt, (DateTimeOffset?)null)
                .SetProperty(j => j.AttemptCount, j => j.AttemptCount + 1), ct);
        if (claimed == 0) return false;

        try
        {
            var outcome = await DerivedDataJobs.RunClaimedAsync(uow, userId, jobId, opt, time, ct);
            if (outcome == JobRunOutcome.Busy) await ReleaseAsync(db, jobId); // instance khác đang dựng cho user này: trả về Pending, không tính lần thử
            return outcome == JobRunOutcome.Done;
        }
        catch (OperationCanceledException)
        {
            await CleanupAsync(uow);
            await ReleaseAsync(db, jobId); // tắt app giữa chừng: không phải lỗi của job
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Derived data job {JobId} failed for user {UserId}", jobId, userId);
            await CleanupAsync(uow);
            var message = ex.Message.Length <= 1900 ? ex.Message : ex.Message[..1900];
            var failedAt = time.GetUtcNow();
            await db.Set<RecalculationJob>().Where(j => j.Id == jobId)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.State, JobState.Failed).SetProperty(j => j.LastError, message).SetProperty(j => j.FinishedAt, failedAt), CancellationToken.None);
            return false;
        }
    }

    private static async Task CleanupAsync(IUnitOfWork uow)
    {
        try { await uow.RollbackTransactionAsync(CancellationToken.None); } catch { /* transaction có thể đã đóng */ }
        uow.DiscardChanges();
    }

    private static Task<int> ReleaseAsync(ApplicationDbContext db, Guid jobId)
        => db.Set<RecalculationJob>().Where(j => j.Id == jobId && j.State == JobState.Running)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.State, JobState.Pending).SetProperty(j => j.AttemptCount, j => j.AttemptCount - 1), CancellationToken.None);
}
