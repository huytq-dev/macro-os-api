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

    /// <summary>
    /// Tính TrendWeightKg cho danh sách dòng cân (đã tracked, chưa xoá): cùng kết quả <see cref="RefreshAsync"/> nhưng không đọc DB.
    /// Trả ngày cân MUỘN NHẤT có TrendWeightKg thực sự đổi (null nếu không dòng nào đổi): EMA trend kéo thay đổi sang các lần cân SAU (giảm dần tới khi làm tròn 2 chữ số hết đổi),
    /// nên lần dựng lại phải coi tới ngày đó là "dữ liệu đã sửa" thì mới được dừng sớm khi hội tụ.
    /// </summary>
    public static DateOnly? Apply(IReadOnlyCollection<WeightEntry> entries, int trendDays)
    {
        var trends = WeightTrendCalculator.Compute(entries.Select(e => (e.LogDate, e.WeightKg)), trendDays);
        DateOnly? lastChanged = null;
        foreach (var e in entries)
        {
            var before = e.TrendWeightKg;
            Stamp(e, trends);
            if (e.TrendWeightKg != before && (lastChanged is null || e.LogDate > lastChanged)) lastChanged = e.LogDate;
        }
        return lastChanged;
    }

    /// <summary>
    /// Trend cân đổi từ <paramref name="fromDate"/> → dựng lại expenditure từ đó: đồng bộ phần gần (hội tụ + trần số dòng) trước khi API trả về, phần còn lại/lỗi thành job nền bền (UC-F03).
    /// <paramref name="trendChangedTo"/> = ngày cân muộn nhất có trend đổi (<see cref="Apply"/>): hội tụ chỉ được xét sau ngày đó + W, vì cửa sổ các ngày tới đó vẫn chứa trend đã đổi.
    /// Trước đây chỉ truyền <paramref name="fromDate"/>: với lần cân kế tiếp cách xa hơn ~6 tuần, lần dựng có thể "hội tụ" trước lần cân đó và để snapshot từ đó trở đi cũ.
    /// </summary>
    public static Task RebuildExpenditureAsync(IUnitOfWork uow, Guid userId, DateOnly fromDate, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct, DateOnly? trendChangedTo = null)
        => DerivedDataSync.RunAsync(uow, userId, fromDate, trendChangedTo is { } to && to > fromDate ? to : fromDate, null, RecalculationCause.WeightChanged, opt, tp, ct);
}
