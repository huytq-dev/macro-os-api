namespace Application;

/// <summary>
/// Energy Balance (hàm thuần, không DB): kcal ăn của từng ngày so với expenditure và target của ngày đó, thống kê khoảng, bucket Day/Week/Month, Relative 3/7/14/30/90 ngày.
/// QUY TẮC TRUNG BÌNH (khớp MacroFactor: 3-day −819 = (−2332 −958 +832) ÷ 3; 14/30/90-day bằng 7-day khi chỉ có 4 ngày log): trung bình MỖI NGÀY ĐÃ LOG HỢP LỆ
/// (có CaloriesKcal, không IsIncomplete; nhịn ăn là ngày log 0 kcal), không chia cho độ dài cửa sổ.
/// </summary>
public static class EnergyBalanceCalculator
{
    public static readonly int[] Windows = [3, 7, 14, 30, 90];

    public const string Deficit = "Deficit", Surplus = "Surplus", Balanced = "Balanced";
    public const string BelowTarget = "BelowTarget", AboveTarget = "AboveTarget", OnTarget = "OnTarget";
    public const string Waiting = "Waiting";

    /// <param name="logs">Ngày đã log (gồm nhịn ăn) theo <c>FoodLogSupport.LoadDaysAsync(includeFasting: true)</c>; ngày vắng = chưa log.</param>
    /// <param name="expenditure">TDEE đã làm tròn theo ngày (chuỗi liên tục của <see cref="ExpenditureSeries"/>); ngày vắng = chưa có.</param>
    /// <param name="target">Target kcal của đúng ngày đó; null = chưa có.</param>
    /// <returns>Mọi ngày liên tục từ <paramref name="from"/> đến <paramref name="to"/>.</returns>
    public static List<EnergyBalanceDay> Days(DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs,
        IReadOnlyDictionary<DateOnly, decimal> expenditure, Func<DateOnly, decimal?> target)
    {
        var days = new List<EnergyBalanceDay>(Math.Max(0, to.DayNumber - from.DayNumber + 1));
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            logs.TryGetValue(d, out var log);
            decimal? calories = log is null ? null : Round(log.TotalCalories);
            decimal? exp = expenditure.TryGetValue(d, out var e) ? e : null;
            decimal? tgt = target(d) is { } t ? Round(t) : null;
            days.Add(new EnergyBalanceDay(d, calories, exp, tgt, calories - exp, calories - tgt, log?.IsFasting ?? false, log?.IsIncomplete ?? false));
        }
        return days;
    }

    /// <summary>Ngày được tính vào mọi thống kê: có số kcal và không đánh dấu chưa đủ.</summary>
    public static bool Counts(EnergyBalanceDay d) => d.CaloriesKcal.HasValue && !d.IsIncomplete;

    public static EnergyBalanceSummary Summary(IReadOnlyList<EnergyBalanceDay> days, DateOnly from, DateOnly to)
    {
        var valid = days.Where(d => Counts(d) && d.Date >= from && d.Date <= to).ToList();
        var vsExp = valid.Where(d => d.VsExpenditureKcal.HasValue).Select(d => d.VsExpenditureKcal!.Value).ToList();
        var vsTgt = valid.Where(d => d.VsTargetKcal.HasValue).Select(d => d.VsTargetKcal!.Value).ToList();
        var avgVsExp = Avg(vsExp);
        var avgVsTgt = Avg(vsTgt);
        return new EnergyBalanceSummary(from, to, valid.Count,
            Avg(valid.Select(d => d.CaloriesKcal!.Value)),
            Avg(valid.Where(d => d.ExpenditureKcal.HasValue).Select(d => d.ExpenditureKcal!.Value)),
            Avg(valid.Where(d => d.TargetKcal.HasValue).Select(d => d.TargetKcal!.Value)),
            avgVsExp, vsExp.Count == 0 ? null : vsExp.Sum(), vsExp.Count, ExpenditureLabel(avgVsExp),
            avgVsTgt, vsTgt.Count, TargetLabel(avgVsTgt));
    }

    public static List<EnergyBalanceBucket> Buckets(IReadOnlyList<EnergyBalanceDay> days, MetricGranularity granularity, DayOfWeek weekStart)
        => days.Where(Counts).GroupBy(d => MetricSeries.BucketStart(d.Date, granularity, weekStart)).OrderBy(g => g.Key)
            .Select(g => new EnergyBalanceBucket(g.Key, MetricSeries.BucketEnd(g.Key, granularity), g.Count(),
                Avg(g.Select(d => d.CaloriesKcal!.Value))!.Value,
                Avg(g.Where(d => d.ExpenditureKcal.HasValue).Select(d => d.ExpenditureKcal!.Value)),
                Avg(g.Where(d => d.TargetKcal.HasValue).Select(d => d.TargetKcal!.Value)),
                Avg(g.Where(d => d.VsExpenditureKcal.HasValue).Select(d => d.VsExpenditureKcal!.Value)),
                Avg(g.Where(d => d.VsTargetKcal.HasValue).Select(d => d.VsTargetKcal!.Value))))
            .ToList();

    /// <param name="days">Phải phủ ít nhất 90 ngày kết thúc ở <paramref name="today"/> (ngày ngoài cửa sổ bị bỏ).</param>
    /// <param name="toTarget">false = Relative to Expenditure, true = Relative to Targets.</param>
    public static List<EnergyBalanceRelative> Relative(IReadOnlyList<EnergyBalanceDay> days, DateOnly today, bool toTarget)
        => Windows.Select(n =>
        {
            var vs = days.Where(d => Counts(d) && d.Date <= today && d.Date > today.AddDays(-n))
                .Select(d => toTarget ? d.VsTargetKcal : d.VsExpenditureKcal).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            var avg = Avg(vs);
            return new EnergyBalanceRelative(n, avg, vs.Count, toTarget ? TargetLabel(avg) : ExpenditureLabel(avg));
        }).ToList();

    public static string ExpenditureLabel(decimal? avg) => avg is not { } a ? Waiting : a == 0 ? Balanced : a < 0 ? Deficit : Surplus;
    public static string TargetLabel(decimal? avg) => avg is not { } a ? Waiting : a == 0 ? OnTarget : a < 0 ? BelowTarget : AboveTarget;

    private static decimal? Avg(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Round(list.Average());
    }

    private static decimal Round(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);
}
