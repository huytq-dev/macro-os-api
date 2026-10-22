namespace Application;

/// <summary>
/// Hàng đợi bền dựng lại dữ liệu dẫn xuất trên bảng <see cref="RecalculationJob"/> có sẵn (không cần migration): xếp job, chạy job đã nhận dưới khoá user, tính trạng thái cho FE.
/// Bất biến: (1) mỗi user có tối đa một lần dựng tại một thời điểm (khoá <see cref="DerivedDataSync.LockResource"/>); (2) job đọc dữ liệu nguồn SAU khi giữ khoá nên job cũ đến trễ vẫn tính từ nguồn mới nhất
/// và không đè kết quả mới; (3) snapshot do phiên bản thuật toán mới hơn dựng thì không bị ghi đè (xem <see cref="ExpenditureRebuilder"/>).
/// </summary>
public static class DerivedDataJobs
{
    private const int MaxErrorLength = 1900;
    private const int MaxAggregateDays = 400;

    /// <summary>
    /// Xếp job Pending cho user: gộp vào job Pending đang có (khoảng ngày hợp lại) nếu có, không thì tạo mới. Lưu ngay. <paramref name="error"/> là lý do (lỗi dựng đồng bộ) nếu có.
    /// FromDate = ngày đầu cần dựng lại snapshot; ToDate = ngày cuối của dữ liệu đã sửa (dùng làm mới aggregate dinh dưỡng với job FoodLogChanged).
    /// </summary>
    public static async Task EnqueueAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, RecalculationCause cause, string? error, CancellationToken ct)
    {
        var repo = uow.Repository<RecalculationJob>();
        var pending = await repo.Query(tracking: true).Where(j => j.UserId == userId && j.State == JobState.Pending).FirstOrDefaultAsync(ct);
        if (pending is null)
        {
            await repo.AddAsync(new RecalculationJob
            {
                Id = Guid.NewGuid(), UserId = userId, Cause = cause, FromDate = from, ToDate = to < from ? from : to, State = JobState.Pending,
                AlgorithmVersion = AlgorithmVersions.Expenditure, LastError = Trim(error), // nhãn thực được ghi lại khi job chạy xong
            }, ct);
        }
        else
        {
            if (from < pending.FromDate) pending.FromDate = from;
            if (to > pending.ToDate) pending.ToDate = to;
            if (error is not null) pending.LastError = Trim(error);
        }
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Chạy job worker đã nhận (State = Running): lấy khoá user, ĐỌC LẠI nguồn dưới khoá, gộp mọi job Pending của user vào một lần dựng từ ngày sớm nhất đến hôm nay, rồi đánh dấu tất cả Succeeded
    /// trong cùng transaction với snapshot. Lỗi thì ném ra: caller rollback và ghi Failed cho job đã nhận.
    /// </summary>
    public static async Task<JobRunOutcome> RunClaimedAsync(IUnitOfWork uow, Guid userId, Guid claimedJobId, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        if (!await uow.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(userId), opt.DerivedWorkerLockWaitMs, ct))
        {
            await uow.RollbackTransactionAsync(ct);
            return JobRunOutcome.Busy;
        }

        var jobs = await uow.Repository<RecalculationJob>().Query(tracking: true)
            .Where(j => j.UserId == userId && (j.Id == claimedJobId || j.State == JobState.Pending))
            .ToListAsync(ct);
        if (jobs.Count > 0)
        {
            var from = jobs.Min(j => j.FromDate);

            // Aggregate dinh dưỡng của các ngày job FoodLogChanged đã sửa (bình thường đã làm đồng bộ; đây là đường bù khi đồng bộ lỗi)
            var dates = jobs.Where(j => j.Cause == RecalculationCause.FoodLogChanged)
                .SelectMany(j => Enumerable.Range(0, Math.Min(j.ToDate.DayNumber - j.FromDate.DayNumber, MaxAggregateDays) + 1).Select(j.FromDate.AddDays))
                .Distinct().ToList();
            if (dates.Count > 0) await NutritionAggregates.RefreshAsync(uow, userId, dates, tp, ct);

            var localToday = (await UserLocale.GetAsync(uow, userId, tp, ct)).Today;
            await ExpenditureRebuilder.RebuildAsync(uow, userId, from, localToday, opt, ct, null, localToday);

            var now = tp.GetUtcNow();
            foreach (var j in jobs) { j.State = JobState.Succeeded; j.FinishedAt = now; j.LastError = null; j.AlgorithmVersion = AlgorithmVersions.ExpenditureFor(opt); }
            await uow.SaveChangesAsync(ct);
        }
        await uow.CommitTransactionAsync(ct);
        return JobRunOutcome.Done;
    }

    /// <summary>Trạng thái dựng lại của user cho FE: Ready (không còn job), Updating (còn job Pending/Running hoặc Failed còn lượt thử), Failed (có job hết lượt thử).</summary>
    public static async Task<DerivedDataStatusResponse> StatusAsync(IUnitOfWork uow, Guid userId, AlgorithmOptions opt, CancellationToken ct)
    {
        var open = await uow.Repository<RecalculationJob>().Query()
            .Where(j => j.UserId == userId && j.State != JobState.Succeeded)
            .ToListAsync(ct);
        if (open.Count == 0)
        {
            var last = await uow.Repository<RecalculationJob>().Query()
                .Where(j => j.UserId == userId && j.State == JobState.Succeeded)
                .OrderByDescending(j => j.FinishedAt)
                .Select(j => j.FinishedAt)
                .FirstOrDefaultAsync(ct);
            return new DerivedDataStatusResponse(DerivedDataStatusResponse.Ready, null, null, 0, last);
        }

        var failed = open.Any(j => j.State == JobState.Failed && j.AttemptCount >= opt.DerivedJobMaxAttempts);
        return new DerivedDataStatusResponse(
            failed ? DerivedDataStatusResponse.Failed : DerivedDataStatusResponse.Updating,
            open.Min(j => j.FromDate), open.Max(j => j.ToDate), open.Max(j => j.AttemptCount),
            open.Max(j => j.FinishedAt ?? j.StartedAt ?? j.ModifiedAt ?? j.CreatedAt));
    }

    private static string? Trim(string? error) => error is null ? null : error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
}
