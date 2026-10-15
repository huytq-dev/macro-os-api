namespace Application;

/// <param name="KcalIn">Tổng kcal ngày; null = chưa log.</param>
/// <param name="TrendKg">Trend weight ngày có cân; null = không cân.</param>
public sealed record ExpenditureDay(DateOnly Date, decimal? KcalIn, decimal? TrendKg);

/// <param name="TdeeRaw">Ước lượng thô theo CICO ngược trong cửa sổ.</param>
public sealed record ExpenditureRaw(decimal TdeeRaw, decimal AvgKcalIn, decimal RateKgPerWeek, int LoggedDays);

/// <summary>ALG-03 (thuần, không DB): TDEE_raw = mean(kcal_in) − Δtrend × kcalPerKg / số ngày; làm mượt EMA β.</summary>
public static class ExpenditureCalculator
{
    /// <summary>Null khi chưa đủ dữ liệu: &lt; 2 điểm trend hoặc không ngày nào log trong cửa sổ.</summary>
    public static ExpenditureRaw? ComputeRaw(IReadOnlyList<ExpenditureDay> window, int kcalPerKg)
    {
        var logged = window.Where(d => d.KcalIn.HasValue).Select(d => d.KcalIn!.Value).ToList();
        var weighed = window.Where(d => d.TrendKg.HasValue).ToList();
        if (logged.Count == 0 || weighed.Count < 2) return null;

        var avgKcal = logged.Average(); // ngày thiếu = trung bình ngày đã log (BR sai số ~13 %)
        var first = weighed[0]; var last = weighed[^1];
        var spanDays = last.Date.DayNumber - first.Date.DayNumber;
        if (spanDays <= 0) return null;

        var deltaKg = last.TrendKg!.Value - first.TrendKg!.Value;
        var tdeeRaw = avgKcal - deltaKg * kcalPerKg / spanDays;
        var rate = deltaKg / spanDays * 7;
        return new ExpenditureRaw(Math.Round(tdeeRaw, 2), Math.Round(avgKcal, 2), Math.Round(rate, 3), logged.Count);
    }

    public static decimal Smooth(decimal prevTdee, decimal rawTdee, decimal beta)
        => Math.Round(beta * rawTdee + (1 - beta) * prevTdee, 2);

    /// <summary>ST-01: Calibrating &lt; calibratingDays kể từ start; Updating khi 7 ngày gần nhất ≤ 3 ngày thiếu log và ≥ 1 lần cân; else Holding.</summary>
    public static ExpenditureStatus Status(DateOnly date, DateOnly startDate, int calibratingDays, IReadOnlyList<ExpenditureDay> last7)
    {
        if (date.DayNumber - startDate.DayNumber < calibratingDays) return ExpenditureStatus.Calibrating;
        return HoldingReason(last7) is null ? ExpenditureStatus.Updating : ExpenditureStatus.Holding;
    }

    /// <summary>Lý do Holding cho dashboard; null nếu đủ điều kiện Updating.</summary>
    public static string? HoldingReason(IReadOnlyList<ExpenditureDay> last7)
    {
        var missing = last7.Count(d => !d.KcalIn.HasValue) + Math.Max(0, 7 - last7.Count);
        var weighed = last7.Any(d => d.TrendKg.HasValue);
        if (missing <= 3 && weighed) return null;
        return string.Join("; ", new[]
        {
            missing > 3 ? $"Thiếu log {missing}/7 ngày gần nhất (tối đa 3)" : null,
            !weighed ? "Không có lần cân nào trong 7 ngày" : null,
        }.Where(s => s is not null));
    }
}
