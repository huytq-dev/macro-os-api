namespace Application;

/// <summary>
/// Trang chi tiết nutrient (hàm thuần, không DB): chuỗi ngày liên tục, thống kê khoảng, bucket Day/Week/Month, trung bình Past Week / Past Month, Contributors và Nutrient Timing.
/// QUY TẮC TRUNG BÌNH (khớp MacroFactor: 272 + 100 + 1474 + 3258 = 5104 ÷ 4 = 1276): trung bình MỖI NGÀY ĐÃ LOG (có Amount, không IsIncomplete), không chia cho độ dài cửa sổ.
/// </summary>
public static class NutrientDetailCalculator
{
    /// <summary>Chất mà ngày nhịn ăn tính là 0 (không ăn gì = đúng 0): macro, fiber, sodium, net carbs. Chất khác ở ngày nhịn ăn không có dữ liệu (null).</summary>
    public static readonly string[] ZeroOnFastingCodes = [.. NutrientCatalog.MacroCodes, "fiber_g", "sodium_mg", NutrientCatalog.NetCarbsCode];

    /// <summary>Số chữ số thập phân của lượng: cột snapshot macro/fiber/sodium 2, vi chất 4 (<see cref="NutritionRounding"/>).</summary>
    public static int Decimals(string code) => ZeroOnFastingCodes.Contains(code) ? NutritionRounding.Macro : NutritionRounding.Micro;

    /// <param name="logs">Ngày đã log (gồm nhịn ăn); ngày vắng = chưa log.</param>
    /// <param name="loggedAmount">Lượng của một ngày đã log (không nhịn ăn); null = ngày đó không món nào có dữ liệu chất này.</param>
    /// <param name="goal">Floor/Target/Ceiling của đúng ngày đó.</param>
    /// <returns>Mọi ngày liên tục từ <paramref name="from"/> đến <paramref name="to"/>.</returns>
    public static List<NutrientSeriesDay> Days(DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs, Func<DateOnly, decimal?> loggedAmount,
        bool zeroOnFasting, Func<DateOnly, (decimal? Floor, decimal? Target, decimal? Ceiling)> goal, int decimals)
    {
        var days = new List<NutrientSeriesDay>(Math.Max(0, to.DayNumber - from.DayNumber + 1));
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            logs.TryGetValue(d, out var log);
            decimal? amount = log is null ? null
                : log.IsFasting ? (zeroOnFasting ? 0m : null)
                : loggedAmount(d) is { } a ? NutritionRounding.Round(a, decimals) : null;
            var (floor, target, ceiling) = goal(d);
            days.Add(new NutrientSeriesDay(d, amount, floor, target, ceiling, log?.IsFasting ?? false, log?.IsIncomplete ?? false));
        }
        return days;
    }

    /// <summary>Ngày được tính vào mọi thống kê: có Amount và không đánh dấu chưa đủ.</summary>
    public static bool Counts(NutrientSeriesDay d) => d.Amount.HasValue && !d.IsIncomplete;

    public static NutrientSeriesSummary Summary(IReadOnlyList<NutrientSeriesDay> days, DateOnly from, DateOnly to, int decimals)
    {
        var valid = days.Where(d => Counts(d) && d.Date >= from && d.Date <= to).ToList();
        if (valid.Count == 0) return new NutrientSeriesSummary(from, to, 0, null, null, null, null);
        var amounts = valid.Select(d => d.Amount!.Value).ToList();
        return new NutrientSeriesSummary(from, to, valid.Count, Round(amounts.Average(), decimals), amounts.Min(), amounts.Max(),
            Avg(valid.Where(d => d.Target.HasValue).Select(d => d.Target!.Value), decimals));
    }

    public static List<NutrientSeriesBucket> Buckets(IReadOnlyList<NutrientSeriesDay> days, MetricGranularity granularity, DayOfWeek weekStart, int decimals)
        => days.Where(Counts).GroupBy(d => MetricSeries.BucketStart(d.Date, granularity, weekStart)).OrderBy(g => g.Key)
            .Select(g => new NutrientSeriesBucket(g.Key, MetricSeries.BucketEnd(g.Key, granularity), g.Count(),
                Round(g.Average(d => d.Amount!.Value), decimals), Avg(g.Where(d => d.Target.HasValue).Select(d => d.Target!.Value), decimals)))
            .ToList();

    /// <summary>Trung bình mỗi ngày đã log trong cửa sổ <paramref name="windowDays"/> ngày kết thúc ở <paramref name="end"/> (gồm end); null khi không có ngày nào dùng được.</summary>
    public static (decimal? Average, int LoggedDays) WindowAverage(IReadOnlyList<NutrientSeriesDay> days, DateOnly end, int windowDays, int decimals)
    {
        var valid = days.Where(d => Counts(d) && d.Date <= end && d.Date > end.AddDays(-windowDays)).ToList();
        return (valid.Count == 0 ? null : Round(valid.Average(d => d.Amount!.Value), decimals), valid.Count);
    }

    /// <summary>Entry có đóng góp &gt; 0, lượng giảm dần rồi theo tên (ổn định).</summary>
    public static List<NutrientContributorModel> Contributors(IEnumerable<(Guid EntryId, string Name, decimal? Amount)> entries, int decimals)
        => entries.Where(e => e.Amount is > 0)
            .Select(e => new NutrientContributorModel(e.Name, NutritionRounding.Round(e.Amount!.Value, decimals), e.EntryId))
            .OrderByDescending(c => c.Amount).ThenBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.EntryId).ToList();

    /// <summary>Luôn 24 khung giờ 0–23: tổng lượng của các entry có giờ ăn trong khung đó (0 nếu không có).</summary>
    public static List<NutrientTimingModel> Timing(IEnumerable<(TimeOnly Time, decimal? Amount)> entries, int decimals)
    {
        var byHour = entries.Where(e => e.Amount is > 0).GroupBy(e => e.Time.Hour).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount!.Value));
        return Enumerable.Range(0, 24).Select(h => new NutrientTimingModel(h, NutritionRounding.Round(byHour.GetValueOrDefault(h), decimals))).ToList();
    }

    public static string TargetSource(string basis) => basis switch
    {
        NutrientCatalog.Basis.Program => "Strategy",
        NutrientCatalog.Basis.User => "Custom",
        NutrientCatalog.Basis.None => "None",
        _ => "Default",
    };

    private static decimal? Avg(IEnumerable<decimal> values, int decimals)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Round(list.Average(), decimals);
    }

    private static decimal Round(decimal v, int decimals) => NutritionRounding.Round(v, decimals);
}
