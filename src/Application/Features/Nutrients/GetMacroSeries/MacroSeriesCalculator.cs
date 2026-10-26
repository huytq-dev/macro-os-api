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
            // Số HIỆU LỰC (manual thắng Food Log). Trường chưa biết giữ null: không tạo P/F/C = 0, không gán kcal vào Other (ngày calories-only: giữ kcal, không vẽ macro); Other chỉ khi biết đủ kcal + P + F + C
            var protein = log.Protein * KcalPerGramProtein;
            var fat = log.Fat * KcalPerGramFat;
            var carbs = log.Carbs * KcalPerGramCarbs;
            decimal? other = log.Calories is { } kcal && protein is { } pk && fat is { } fk && carbs is { } ck ? kcal - pk - fk - ck : null;
            days.Add(new MacroSeriesDay(d, true, log.IsFasting, log.IsIncomplete,
                R(log.Calories), R(log.Protein), R(log.Fat), R(log.Carbs), R(protein), R(fat), R(carbs), R(other)));
        }
        return days;
    }

    /// <summary>Ngày được tính vào thống kê: đã log (Food Log, manual hoặc nhịn ăn) và không đánh dấu chưa đủ. Từng chỉ số chỉ lấy các ngày CÓ giá trị đó (xem <see cref="Averages"/>); macro đã nhập vẫn được tính khi kcal chưa biết.</summary>
    public static bool Counts(MacroSeriesDay d) => d.IsLogged && !d.IsIncomplete;

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
        // Mỗi chỉ số trung bình trên CÁC NGÀY CÓ giá trị đó (mẫu số theo từng trường); null không bao giờ thành 0. Không có ngày nào có giá trị ⇒ null
        decimal? Avg(Func<MacroSeriesDay, decimal?> pick)
        {
            var known = counted.Select(pick).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return known.Count == 0 ? null : known.Average();
        }
        bool Full(Func<MacroSeriesDay, decimal?> pick) => counted.All(d => pick(d).HasValue);
        var kcal = Avg(d => d.CaloriesKcal);
        var (pk, fk, ck, ok) = (Avg(d => d.ProteinKcal), Avg(d => d.FatKcal), Avg(d => d.CarbsKcal), Avg(d => d.OtherKcal));
        // Phần trăm chỉ khi MỌI ngày tính đều biết cả kcal lẫn phần đó (không trộn hai tập ngày khác nhau thành một tỉ lệ)
        decimal? Pct(decimal? part, Func<MacroSeriesDay, decimal?> pickPart)
            => kcal > 0 && part is { } p && Full(d => d.CaloriesKcal) && Full(pickPart) ? NutritionRounding.Round(p / kcal.Value * 100m, NutritionRounding.Percent) : null;
        return new MacroAverages(R(kcal), R(Avg(d => d.ProteinG)), R(Avg(d => d.FatG)), R(Avg(d => d.CarbsG)), R(pk), R(fk), R(ck), R(ok),
            Pct(pk, d => d.ProteinKcal), Pct(fk, d => d.FatKcal), Pct(ck, d => d.CarbsKcal), Pct(ok, d => d.OtherKcal));
    }

    private static decimal? R(decimal? v) => v is { } x ? NutritionRounding.Round(x, NutritionRounding.Macro) : null;
}
