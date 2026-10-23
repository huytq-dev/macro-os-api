namespace Application;

/// <summary>
/// Bộ giải quan sát DÙNG CHUNG (hàm thuần): quan sát hợp lệ của engine hiện có theo từng ngày, đúng như <c>ExpenditureRebuilder.cs:35-41</c>:
/// KcalIn = <see cref="FoodLogSupport.LoggedDay.AlgorithmKcal"/> (ngày chưa log = null KHÔNG phải 0; ngày "log chưa đủ" hoặc có entry 0 kcal không phải nhịn ăn = null; nhịn ăn = 0 hợp lệ),
/// TrendKg = trend đã lưu CHỈ ở ngày có cân (ngày nội suy KHÔNG phải quan sát độc lập). Flux đọc đúng danh sách này; rebuilder có thể dùng lại sau khi được duyệt (chưa đổi rebuilder).
/// </summary>
public static class ExpenditureObservationResolver
{
    public static List<ExpenditureDay> Daily(DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs, IReadOnlyDictionary<DateOnly, decimal> trendOnWeighInDays)
        => Enumerable.Range(0, Math.Max(0, to.DayNumber - from.DayNumber + 1))
            .Select(i => from.AddDays(i))
            .Select(d => new ExpenditureDay(d, FoodLogSupport.AlgorithmKcalOn(logs, d), trendOnWeighInDays.TryGetValue(d, out var trend) ? trend : null))
            .ToList();
}
