namespace Application;

/// <summary>
/// Cổng dữ liệu cân nặng (TRA ≥ 3 ngày/20 ngày, ≥ 1 trong 7 ngày, span ≥ 7, guard khoảng trống G = 14) tính tại <paramref name="today"/> (ngày local) từ các lần cân THẬT trong DB:
/// một nguồn dùng chung cho weekly rate của danh sách cân và ETA của Goal Progress (<see cref="WeightTrendSeries.RateGate"/>). Nạp thêm lần cân ngay TRƯỚC cửa sổ để thấy khoảng trống cắt ngang biên.
/// </summary>
public static class WeightRateGateSupport
{
    public static async Task<(string? Reason, int Count, int InLast7Days, int? DaysSinceLast, int SpanDays, int MaxGapDays)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly today, CancellationToken ct)
    {
        var windowStart = today.AddDays(-(WeightTrendSeries.RegressionDays - 1));
        var dates = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= windowStart && w.LogDate <= today)
            .Select(w => w.LogDate).ToListAsync(ct);
        var before = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate < windowStart)
            .OrderByDescending(w => w.LogDate).Select(w => (DateOnly?)w.LogDate).FirstOrDefaultAsync(ct);
        if (before is { } b) dates.Add(b);
        return WeightTrendSeries.RateGate(dates, today);
    }
}
