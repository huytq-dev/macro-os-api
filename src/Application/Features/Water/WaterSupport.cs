namespace Application;

internal static class WaterSupport
{
    // ponytail: nước chỉ đổi water_g của ngày nên chỉ ghi lại aggregate, không dựng lại expenditure (thuật toán không dùng nước); DailyProgressSnapshot.WaterConsumed cập nhật ở lần rebuild kế tiếp
    public static async Task SaveAsync(IUnitOfWork uow, Guid userId, DateOnly date, TimeProvider tp, CancellationToken ct)
    {
        await uow.EnsureSaveAsync(ct);
        await NutritionAggregates.RefreshAsync(uow, userId, [date], tp, ct);
        await uow.SaveChangesAsync(ct);
    }
}
