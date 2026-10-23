namespace Application;

/// <summary>
/// Biểu đồ Macros (hàm thuần, không DB): mỗi ngày LIÊN TỤC kcal xếp chồng protein ×4 + fat ×9 + carbs ×4 + Other, thống kê khoảng và bucket Day/Week/Month.
/// Nguồn là tổng FoodLog đã lưu của từng ngày (cùng nguồn series từng nutrient). Other = kcal tổng − (4P + 9F + 4C) THÔ có dấu, không kẹp, không bịa.
/// QUY TẮC TRUNG BÌNH (như <see cref="NutrientDetailCalculator"/>): trung bình MỖI NGÀY ĐÃ LOG (có số, không IsIncomplete; nhịn ăn là ngày log 0 kcal), không chia cho số ngày lịch.
/// </summary>
public static class MacroSeriesCalculator
{
    public const decimal KcalPerGramProtein = 4m, KcalPerGramFat = 9m, KcalPerGramCarbs = 4m;

    /// <param name="logs">Ngày đã log (gồm nhịn ăn) theo <c>FoodLogSupport.LoadDaysAsync(includeFasting: true)</c>; ngày vắng = chưa log.</param>
    public static List<MacroSeriesDay> Days(DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs)
    {
        var days = new List<MacroSeriesDay>(Math.Max(0, to.DayNumber - from.DayNumber + 1));
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (!logs.TryGetValue(d, out var log)) { days.Add(new MacroSeriesDay(d, false, false, false, null, null, null, null, null, null, null, null)); continue; }
            var protein = log.TotalProtein * KcalPerGramProtein;
            var fat = log.TotalFat * KcalPerGramFat;
            var carbs = log.TotalCarbs * KcalPerGramCarbs;
            days.Add(new MacroSeriesDay(d, true, log.IsFasting, log.IsIncomplete,
                R(log.TotalCalories), R(log.TotalProtein), R(log.TotalFat), R(log.TotalCarbs), R(protein), R(fat), R(carbs), R(log.TotalCalories - protein - fat - carbs)));
        }
        return days;
    }

    /// <summary>Ngày được tính vào mọi thống kê: đã log (có số) và không đánh dấu chưa đủ.</summary>
    public static bool Counts(MacroSeriesDay d) => d.IsLogged && d.CaloriesKcal.HasValue && !d.IsIncomplete;

    public static MacroSeriesSummary Summary(IReadOnlyList<MacroSeriesDay> days, DateOnly from, DateOnly to)
    {
        var valid = days.Where(d => Counts(d) && d.Date >= from && d.Date <= to).ToList();
        return new MacroSeriesSummary(from, to, valid.Count, Math.Max(0, to.DayNumber - from.DayNumber + 1), Averages(valid));
    }

    public static List<MacroSeriesBucket> Buckets(IReadOnlyList<MacroSeriesDay> days, MetricGranularity granularity, DayOfWeek weekStart)
        => days.Where(Counts).GroupBy(d => MetricSeries.BucketStart(d.Date, granularity, weekStart)).OrderBy(g => g.Key)
            .Select(g => new MacroSeriesBucket(g.Key, MetricSeries.BucketEnd(g.Key, granularity), g.Count(), Averages(g.ToList())))
            .ToList();

    public static MacroAverages Averages(IReadOnlyCollection<MacroSeriesDay> counted)
    {
        if (counted.Count == 0) return new MacroAverages(null, null, null, null, null, null, null, null, null, null, null, null);
        decimal Avg(Func<MacroSeriesDay, decimal?> pick) => counted.Average(d => pick(d)!.Value);
        var kcal = Avg(d => d.CaloriesKcal);
        var (pk, fk, ck, ok) = (Avg(d => d.ProteinKcal), Avg(d => d.FatKcal), Avg(d => d.CarbsKcal), Avg(d => d.OtherKcal));
        decimal? Pct(decimal part) => kcal > 0 ? NutritionRounding.Round(part / kcal * 100m, NutritionRounding.Percent) : null;
        return new MacroAverages(R(kcal), R(Avg(d => d.ProteinG)), R(Avg(d => d.FatG)), R(Avg(d => d.CarbsG)), R(pk), R(fk), R(ck), R(ok), Pct(pk), Pct(fk), Pct(ck), Pct(ok));
    }

    private static decimal R(decimal v) => NutritionRounding.Round(v, NutritionRounding.Macro);
}
