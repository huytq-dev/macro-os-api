namespace Application;

/// <summary>Thống kê khoảng + gộp theo Day/Week/Month cho một chuỗi (ngày, giá trị): dùng chung cho Scale Weight và Visual Body Fat. Hàm thuần.</summary>
public static class MetricSeries
{
    /// <param name="points">Mỗi ngày tối đa một điểm; điểm ngoài [from, to] bị bỏ.</param>
    /// <param name="granularity">null = không dựng bucket (Buckets rỗng); Summary luôn có.</param>
    /// <param name="weekStart">Ngày đầu tuần của user (Week bucket).</param>
    public static (MetricSummary Summary, List<MetricBucket> Buckets) Build(
        IEnumerable<(DateOnly Date, decimal Value)> points, DateOnly from, DateOnly to, MetricGranularity? granularity, DayOfWeek weekStart)
    {
        var inRange = points.Where(p => p.Date >= from && p.Date <= to).OrderBy(p => p.Date).ToList();
        if (inRange.Count == 0)
            return (new MetricSummary(from, to, 0, null, null, null, null, null, null), []);

        var first = inRange[0];
        var last = inRange[^1];
        var summary = new MetricSummary(
            from, to, inRange.Count,
            Average: Round(inRange.Average(p => p.Value)),
            Difference: inRange.Count >= 2 ? Round(last.Value - first.Value) : null,
            First: first.Value, Last: last.Value,
            Min: inRange.Min(p => p.Value), Max: inRange.Max(p => p.Value));

        var buckets = granularity is not { } g ? []
            : inRange.GroupBy(p => BucketStart(p.Date, g, weekStart)).OrderBy(b => b.Key)
                .Select(b => new MetricBucket(b.Key, BucketEnd(b.Key, g), b.Count(), Round(b.Average(p => p.Value))))
                .ToList();
        return (summary, buckets);
    }

    public static DateOnly BucketStart(DateOnly date, MetricGranularity granularity, DayOfWeek weekStart) => granularity switch
    {
        MetricGranularity.Week => date.AddDays(-(((int)date.DayOfWeek - (int)weekStart + 7) % 7)),
        MetricGranularity.Month => new DateOnly(date.Year, date.Month, 1),
        _ => date,
    };

    public static DateOnly BucketEnd(DateOnly start, MetricGranularity granularity) => granularity switch
    {
        MetricGranularity.Week => start.AddDays(6),
        MetricGranularity.Month => new DateOnly(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month)),
        _ => start,
    };

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
