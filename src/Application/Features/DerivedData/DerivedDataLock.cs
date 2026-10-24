namespace Application;

/// <summary>
/// Chạy một ghi đè snapshot/ngày bắt đầu expenditure dưới CÙNG khoá user với dựng lại dẫn xuất (<see cref="DerivedDataSync.LockResource"/>): một lần dựng/worker đang giữ khoá thì ghi này chờ (tối đa <c>DerivedLockWaitMs</c>)
/// rồi trả 409 thử lại được, thay vì chen vào giữa và bị dựng lại ghi đè (hoặc ghi đè dựng lại). Cùng khuôn với <c>SetExpenditureModeCommandHandler</c>.
/// </summary>
public static class DerivedDataLock
{
    /// <param name="busyMessage">Thông điệp 409 khi khoá bận quá thời gian chờ (mặc định ExpenditureSettingsBusy).</param>
    /// <param name="busyErrors">errors của 409 đó (vd reasonCode cho FE); null = không có.</param>
    public static async Task<T> RunAsync<T>(IUnitOfWork uow, Guid userId, AlgorithmOptions opt, Func<Task<T>> work, CancellationToken ct,
        string? busyMessage = null, IDictionary<string, string[]>? busyErrors = null)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            if (!await uow.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(userId), opt.DerivedLockWaitMs, ct))
                throw new ConflictException(busyMessage ?? ExceptionMessages.ExpenditureSettingsBusy, busyErrors);

            var result = await work();
            await uow.CommitTransactionAsync(ct);
            return result;
        }
        catch
        {
            try { await uow.RollbackTransactionAsync(CancellationToken.None); } catch { /* transaction có thể đã đóng */ }
            uow.DiscardChanges();
            throw;
        }
    }
}
