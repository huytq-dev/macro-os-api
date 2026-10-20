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

    /// <summary>Tính TrendWeightKg cho danh sách dòng cân (đã tracked, chưa xoá): cùng kết quả <see cref="RefreshAsync"/> nhưng không đọc DB.</summary>
    public static void Apply(IReadOnlyCollection<WeightEntry> entries, int trendDays)
    {
        var trends = WeightTrendCalculator.Compute(entries.Select(e => (e.LogDate, e.WeightKg)), trendDays);
        foreach (var e in entries)
            e.TrendWeightKg = trends.TryGetValue(e.LogDate, out var t) ? t : null;
    }

    /// <summary>Trend cân đổi từ <paramref name="fromDate"/> → dựng lại expenditure từ đó đến hôm nay (UC-F03): nền nếu có, không thì đồng bộ rồi lưu.</summary>
    public static async Task RebuildExpenditureAsync(IUnitOfWork uow, Guid userId, DateOnly fromDate, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct)
    {
        if (opt.RebuildDerivedInBackground && uow.Derived.IsAvailable)
        {
            uow.Derived.Schedule(userId, fromDate, null);
            return;
        }
        await ExpenditureRebuilder.RebuildAsync(uow, userId, fromDate, FoodLogSupport.Today(tp), opt, ct);
        await uow.SaveChangesAsync(ct);
    }
}
