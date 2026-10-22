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
            Stamp(e, trends);
    }

    /// <summary>Ghi trend của dòng cân và nhãn phiên bản Weight Trend đã tính ra nó; dòng không được tính lại (không nằm trong danh sách) giữ nhãn cũ (Legacy nếu null).</summary>
    private static void Stamp(WeightEntry e, IReadOnlyDictionary<DateOnly, decimal> trends)
    {
        e.TrendWeightKg = trends.TryGetValue(e.LogDate, out var t) ? t : null;
        e.TrendAlgorithmVersion = AlgorithmVersions.WeightTrend;
    }

    /// <summary>Tính TrendWeightKg cho danh sách dòng cân (đã tracked, chưa xoá): cùng kết quả <see cref="RefreshAsync"/> nhưng không đọc DB.</summary>
    public static void Apply(IReadOnlyCollection<WeightEntry> entries, int trendDays)
    {
        var trends = WeightTrendCalculator.Compute(entries.Select(e => (e.LogDate, e.WeightKg)), trendDays);
        foreach (var e in entries)
            Stamp(e, trends);
    }

    /// <summary>
    /// Trend cân đổi từ <paramref name="fromDate"/> → dựng lại expenditure từ đó: đồng bộ phần gần (hội tụ + trần số dòng) trước khi API trả về, phần còn lại/lỗi thành job nền bền (UC-F03).
    /// </summary>
    public static Task RebuildExpenditureAsync(IUnitOfWork uow, Guid userId, DateOnly fromDate, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct)
        => DerivedDataSync.RunAsync(uow, userId, fromDate, fromDate, null, RecalculationCause.WeightChanged, opt, tp, ct);
}
