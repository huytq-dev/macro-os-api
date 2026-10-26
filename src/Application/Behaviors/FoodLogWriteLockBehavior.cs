namespace Application;

/// <summary>
/// Mọi lệnh GHI Food Log của MỘT user chạy lần lượt (khoá theo user, không phải theo ngày): nhiều lần thêm món cùng lúc (bấm "+" liên tiếp, nhiều tab, retry) trước đây cùng tạo dòng FoodLog của ngày
/// (đụng UNIQUE UserId+LogDate), cùng cập nhật tổng ngày của dòng đó (đụng RowVersion) hoặc ghi đè tổng của nhau, nên một phần request lỗi và các món đó KHÔNG được lưu.
/// Khoá <c>sp_getapplock</c> gắn với transaction (an toàn nhiều instance) bọc cả đọc-sửa-ghi của handler; request sau chờ request trước xong rồi mới chạy trên dữ liệu mới nhất.
/// <para>
/// Transaction này chỉ bảo vệ phần ghi NGUỒN: <see cref="FoodLogSupport.SaveAndRebuildAsync"/> commit nó (nhả khoá) NGAY SAU khi lưu nguồn và TRƯỚC khi dựng dữ liệu dẫn xuất
/// (<see cref="DerivedDataSync"/> tự mở transaction + khoá user của nó); lệnh không đi qua đó (nước) được commit ở đây sau handler. Lỗi ở bất kỳ bước nào trước commit thì rollback toàn bộ, không món nào được lưu nửa chừng.
/// </para>
/// </summary>
public sealed class FoodLogWriteLockBehavior<TRequest, TResponse>(IUnitOfWork _uow, IUserContext _userContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public const int WaitMs = 15_000;

    public static string LockResource(Guid userId) => $"macroos:foodlog:{userId:N}";

    /// <summary>Các lệnh ghi dữ liệu nguồn của Food Log (FoodLog, FoodLogEntry, WaterLog, Manual Nutrition, cờ nhịn ăn / chưa đủ). Khai báo ở một chỗ để không phải đánh dấu từng lệnh.</summary>
    private static readonly HashSet<Type> Writes =
    [
        typeof(AddFoodLogEntryCommand), typeof(AddEntriesBatchCommand), typeof(QuickAddCommand), typeof(CopyFoodLogCommand),
        typeof(BulkCopyEntriesCommand), typeof(BulkMoveEntriesCommand), typeof(BulkDeleteEntriesCommand),
        typeof(UpdateFoodLogEntryCommand), typeof(DeleteFoodLogEntryCommand), typeof(ClearDayLogCommand), typeof(QuickEditDayCommand),
        typeof(SaveNutritionDataCommand), typeof(DeleteNutritionDataCommand), typeof(RemoveManualOverrideCommand), typeof(SetDayFastingCommand), typeof(SetDayIncompleteCommand),
        typeof(AddWaterCommand), typeof(DeleteWaterCommand),
    ];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!Writes.Contains(typeof(TRequest))) return await next(ct);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            if (!await _uow.TryAcquireTransactionLockAsync(LockResource(_userContext.UserId), WaitMs, ct))
                throw new ConflictException(ExceptionMessages.ConcurrentWriteConflict, new Dictionary<string, string[]> { ["reasonCode"] = ["CONCURRENT"] });

            var response = await next(ct);
            await _uow.CommitTransactionAsync(ct);   // no-op khi SaveAndRebuildAsync đã commit
            return response;
        }
        catch
        {
            try { await _uow.RollbackTransactionAsync(CancellationToken.None); } catch { /* transaction có thể đã đóng */ }
            _uow.DiscardChanges();
            throw;
        }
    }
}
