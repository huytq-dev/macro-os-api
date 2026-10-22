namespace Application;

/// <summary>
/// Energy Balance (hàm thuần, không DB): kcal ăn của từng ngày so với expenditure và target của ngày đó, thống kê khoảng, bucket Day/Week/Month, Relative 3/7/14/30/90 ngày.
/// QUY TẮC TRUNG BÌNH (khớp MacroFactor: 3-day −819 = (−2332 −958 +832) ÷ 3; 14/30/90-day bằng 7-day khi chỉ có 4 ngày log): trung bình MỖI NGÀY ĐÃ LOG HỢP LỆ
/// (có CaloriesKcal, không IsIncomplete, không phải ngày entry 0 kcal không nhịn ăn; nhịn ăn là ngày log 0 kcal), không chia cho độ dài cửa sổ.
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
    /// <param name="provenance">Provenance của target từng ngày (VERIFIED | LEGACY_UNVERIFIED); null = mọi target coi là VERIFIED (chỉ cho test thuần/nguồn target không phụ thuộc lịch sử program).</param>
    public static List<EnergyBalanceDay> Days(DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs,
        IReadOnlyDictionary<DateOnly, decimal> expenditure, Func<DateOnly, decimal?> target, Func<DateOnly, string?>? provenance = null)
    {
        var days = new List<EnergyBalanceDay>(Math.Max(0, to.DayNumber - from.DayNumber + 1));
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            logs.TryGetValue(d, out var log);
            decimal? calories = log is null ? null : Round(log.TotalCalories);
            decimal? exp = expenditure.TryGetValue(d, out var e) ? e : null;
            decimal? tgt = target(d) is { } t ? Round(t) : null;
            // Ngày bị loại khỏi thống kê (đánh dấu chưa đủ, hoặc có entry nhưng 0 kcal không nhịn ăn) vẫn hiện kcal đã log nhưng KHÔNG được trình bày như thâm hụt/thặng dư thật: vs* = null
            // (cùng quy tắc ngày hợp lệ với Expenditure). FE coi "vs* khác null" là tập ngày được tính của tab đó.
            var excluded = log is not null && (log.IsIncomplete || (!log.IsFasting && calories == 0m));
            days.Add(new EnergyBalanceDay(d, calories, exp, tgt, excluded ? null : calories - exp, excluded ? null : calories - tgt, log?.IsFasting ?? false, log?.IsIncomplete ?? false,
                tgt is null ? null : provenance?.Invoke(d) ?? ProgramTargetResolver.Verified));
        }
        return days;
    }

    /// <summary>Ngày có so với target VÀ target đã xác minh: chỉ ngày này vào thống kê so với target (Summary/Bucket/Relative). Energy Balance so với Expenditure không phụ thuộc target nên không dùng hàm này.</summary>
    public static bool TargetCounts(EnergyBalanceDay d) => d.VsTargetKcal.HasValue && d.TargetProvenance != ProgramTargetResolver.LegacyUnverified;

    /// <summary>Ngày có so với target nhưng target LEGACY_UNVERIFIED: hiển thị tham khảo, loại khỏi thống kê và đếm riêng.</summary>
    public static bool TargetUnverified(EnergyBalanceDay d) => d.VsTargetKcal.HasValue && d.TargetProvenance == ProgramTargetResolver.LegacyUnverified;

    /// <summary>
    /// Ngày được tính vào mọi thống kê: có số kcal, không đánh dấu chưa đủ, và không phải ngày có entry nhưng 0 kcal mà không nhịn ăn (cùng quy tắc ngày hợp lệ
    /// <see cref="FoodLogSupport.LoggedDay.AlgorithmKcal"/> với Expenditure). Ngày 0 kcal đó vẫn hiện 0 trong dải ngày, chỉ không vào trung bình/bucket/relative.
    /// </summary>
    public static bool Counts(EnergyBalanceDay d) => d.CaloriesKcal.HasValue && !d.IsIncomplete && (d.IsFasting || d.CaloriesKcal > 0);

    /// <summary>
    /// Mỗi tab dùng MỘT tập ngày cho cả ba số: Expenditure = ngày hợp lệ có cả kcal lẫn expenditure (vsExpenditure khác null); Targets = ngày hợp lệ có cả kcal lẫn target (vsTarget khác null).
    /// Nên avgCaloriesVsExpenditure − avgExpenditure = avgVsExpenditure (và tương tự cho target) đúng từng đơn vị đã làm tròn. <c>AvgCaloriesKcal</c> (mọi ngày hợp lệ) giữ để tương thích.
    /// </summary>
    public static EnergyBalanceSummary Summary(IReadOnlyList<EnergyBalanceDay> days, DateOnly from, DateOnly to)
    {
        var valid = days.Where(d => Counts(d) && d.Date >= from && d.Date <= to).ToList();
        var expPairs = valid.Where(d => d.VsExpenditureKcal.HasValue).ToList();
        var tgtPairs = valid.Where(TargetCounts).ToList();
        var avgCalExp = Avg(expPairs.Select(d => d.CaloriesKcal!.Value));
        var avgExp = Avg(expPairs.Select(d => d.ExpenditureKcal!.Value));
        var avgCalTgt = Avg(tgtPairs.Select(d => d.CaloriesKcal!.Value));
        var avgTgt = Avg(tgtPairs.Select(d => d.TargetKcal!.Value));
        var avgVsExp = avgCalExp - avgExp;
        var avgVsTgt = avgCalTgt - avgTgt;
        return new EnergyBalanceSummary(from, to, valid.Count,
            Avg(valid.Select(d => d.CaloriesKcal!.Value)),
            avgExp, avgTgt,
            avgVsExp, expPairs.Count == 0 ? null : expPairs.Sum(d => d.VsExpenditureKcal!.Value), expPairs.Count, ExpenditureLabel(avgVsExp),
            avgVsTgt, tgtPairs.Count, TargetLabel(avgVsTgt),
            avgCalExp, avgCalTgt, valid.Count(TargetUnverified));
    }

    public static List<EnergyBalanceBucket> Buckets(IReadOnlyList<EnergyBalanceDay> days, MetricGranularity granularity, DayOfWeek weekStart)
        => days.Where(Counts).GroupBy(d => MetricSeries.BucketStart(d.Date, granularity, weekStart)).OrderBy(g => g.Key)
            .Select(g =>
            {
                var expPairs = g.Where(d => d.VsExpenditureKcal.HasValue).ToList();
                var tgtPairs = g.Where(TargetCounts).ToList();
                var avgCalExp = Avg(expPairs.Select(d => d.CaloriesKcal!.Value));
                var avgExp = Avg(expPairs.Select(d => d.ExpenditureKcal!.Value));
                var avgCalTgt = Avg(tgtPairs.Select(d => d.CaloriesKcal!.Value));
                var avgTgt = Avg(tgtPairs.Select(d => d.TargetKcal!.Value));
                return new EnergyBalanceBucket(g.Key, MetricSeries.BucketEnd(g.Key, granularity), g.Count(),
                    Avg(g.Select(d => d.CaloriesKcal!.Value))!.Value, avgExp, avgTgt, avgCalExp - avgExp, avgCalTgt - avgTgt, avgCalExp, avgCalTgt, g.Count(TargetUnverified));
            })
            .ToList();

    /// <param name="days">Phải phủ ít nhất 90 ngày kết thúc ở <paramref name="today"/> (ngày ngoài cửa sổ bị bỏ).</param>
    /// <param name="toTarget">false = Relative to Expenditure, true = Relative to Targets.</param>
    public static List<EnergyBalanceRelative> Relative(IReadOnlyList<EnergyBalanceDay> days, DateOnly today, bool toTarget)
        => Windows.Select(n =>
        {
            var window = days.Where(d => Counts(d) && d.Date <= today && d.Date > today.AddDays(-n)).ToList();
            // Targets: chỉ ngày có target VERIFIED vào thống kê; ngày LEGACY_UNVERIFIED loại và đếm riêng (UnverifiedDays). Expenditure không phụ thuộc target
            var vs = window.Where(d => toTarget ? TargetCounts(d) : d.VsExpenditureKcal.HasValue)
                .Select(d => toTarget ? d.VsTargetKcal!.Value : d.VsExpenditureKcal!.Value).ToList();
            var avg = Avg(vs);
            return new EnergyBalanceRelative(n, avg, vs.Count, toTarget ? TargetLabel(avg) : ExpenditureLabel(avg), toTarget ? window.Count(TargetUnverified) : 0);
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
