namespace Application;

/// <summary>
/// Thử lại các job đã hết lượt thử (Failed). An toàn và idempotent: chạy dưới khoá dựng lại của user (nhiều tab/nhiều lần bấm xếp hàng tuần tự), gộp mọi job Failed hết lượt thử
/// thành MỘT job Pending (hợp khoảng ngày, gộp vào job Pending sẵn có nếu có) rồi đánh dấu các job Failed đã được thay thế (Succeeded, giữ LastError để tra cứu) ⇒ không bao giờ tạo job trùng;
/// lần gọi sau thấy không còn Failed nên không làm gì. Không lấy được khoá trong thời gian chờ (đang có lần dựng khác) cũng chỉ trả trạng thái hiện tại.
/// </summary>
public sealed class RetryDerivedDataCommandHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<RetryDerivedDataCommand, DerivedDataStatusResponse>
{
    public async Task<DerivedDataStatusResponse> Handle(RetryDerivedDataCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var opt = _options.Value;
        var changed = false;

        await _uow.BeginTransactionAsync(ct);
        try
        {
            if (await _uow.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(userId), opt.DerivedLockWaitMs, ct))
            {
                var repo = _uow.Repository<RecalculationJob>();
                var failed = await repo.Query(tracking: true)
                    .Where(j => j.UserId == userId && j.State == JobState.Failed && j.AttemptCount >= opt.DerivedJobMaxAttempts)
                    .ToListAsync(ct);
                if (failed.Count > 0)
                {
                    var from = failed.Min(j => j.FromDate);
                    var to = failed.Max(j => j.ToDate);
                    var now = _timeProvider.GetUtcNow();
                    foreach (var job in failed) { job.State = JobState.Succeeded; job.FinishedAt = now; }   // đã được job thử lại thay thế; LastError giữ nguyên để tra cứu
                    // Job thử lại giữ nguyên nhân FoodLogChanged nếu có job hỏng mang nó: chỉ job FoodLogChanged làm mới aggregate dinh dưỡng (trước đây luôn Manual ⇒ aggregate của ngày ăn
                    // đã hỏng không bao giờ được làm mới mà trạng thái lại Ready). Gộp/tạo qua EnqueueAsync (gộp có điều kiện, an toàn với worker đang chạy); hàm này lưu luôn các job hỏng ở trên.
                    var cause = failed.Any(j => j.Cause == RecalculationCause.FoodLogChanged) ? RecalculationCause.FoodLogChanged : RecalculationCause.Manual;
                    await DerivedDataJobs.EnqueueAsync(_uow, userId, from, to, cause, null, ct, _timeProvider);
                    changed = true;
                }
            }
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            try { await _uow.RollbackTransactionAsync(CancellationToken.None); } catch { /* transaction có thể đã đóng */ }
            throw;
        }

        if (changed) _uow.Derived.Wake();
        return await DerivedDataJobs.StatusAsync(_uow, userId, opt, ct);
    }
}
