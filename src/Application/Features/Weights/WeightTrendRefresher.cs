namespace Application;

/// <summary>Tính lại TrendWeightKg cho toàn bộ cân nặng của user (đang tracked) sau khi thêm/sửa/xoá.</summary>
public static class WeightTrendRefresher
{
    // ponytail: O(n) cả chuỗi mỗi lần ghi; chuyển sang tính từ ngày đổi khi dãy > vài nghìn ngày
    public static async Task RefreshAsync(IUnitOfWork uow, Guid userId, int trendDays, CancellationToken ct)
    {
        var entries = await uow.Repository<WeightEntry>().GetAllTrackedAsync(w => w.UserId == userId, ct);
        var trends = WeightTrendCalculator.Compute(entries.Select(e => (e.LogDate, e.WeightKg)), trendDays);
        foreach (var e in entries)
            e.TrendWeightKg = trends.TryGetValue(e.LogDate, out var t) ? t : null;
    }
}
