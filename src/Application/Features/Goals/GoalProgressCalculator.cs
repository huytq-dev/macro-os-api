namespace Application;

public static class GoalProgressCalculator
{
    /// <summary>% hoàn thành lose/gain = (Start − hiện tại) ÷ (Start − Target), kẹp 0–100; null nếu thiếu số hoặc Start = Target.</summary>
    public static decimal? ProgressPercent(decimal? start, decimal? target, decimal? current)
    {
        if (start is not { } s || target is not { } t || current is not { } c || s == t) return null;
        return Math.Round(Math.Clamp((s - c) / (s - t) * 100m, 0m, 100m), 1);
    }

    /// <summary>Waterfall: tuần k = 7 ngày từ start + 7k. TrendKg = điểm cuối của tuần; Delta = so tuần trước có dữ liệu (tuần đầu so StartWeightKg).</summary>
    public static List<GoalWeekModel> Weeks(DateOnly start, decimal? startWeight, IReadOnlyList<(DateOnly Date, decimal Kg)> points, DateOnly today)
    {
        var weeks = new List<GoalWeekModel>();
        var previous = startWeight;
        for (var weekStart = start; weekStart <= today; weekStart = weekStart.AddDays(7))
        {
            var weekEnd = weekStart.AddDays(6);
            var inWeek = points.Where(x => x.Date >= weekStart && x.Date <= weekEnd).OrderBy(x => x.Date).ToList();
            decimal? kg = inWeek.Count == 0 ? null : inWeek[^1].Kg;
            decimal? delta = kg is { } now && previous is { } before ? Math.Round(now - before, 2) : null;
            weeks.Add(new GoalWeekModel(weekStart, kg, delta));
            if (kg is not null) previous = kg;
        }
        return weeks;
    }

    /// <summary>Ngày dự kiến chạm target: ngoại suy tuyến tính từ 14 ngày cuối; null nếu dưới 7 ngày dữ liệu, tốc độ 0, đi sai hướng hoặc quá 10 năm.</summary>
    public static DateOnly? Eta(IReadOnlyList<(DateOnly Date, decimal Kg)> points, decimal? target, DateOnly today)
    {
        if (target is not { } t || points.Count < 2) return null;
        var last = points[^1];
        var first = points.First(x => x.Date >= last.Date.AddDays(-14));
        var days = last.Date.DayNumber - first.Date.DayNumber;
        if (days < 7) return null;

        var ratePerDay = (last.Kg - first.Kg) / days;
        var remaining = t - last.Kg;
        if (ratePerDay == 0 || remaining == 0 || Math.Sign(ratePerDay) != Math.Sign(remaining)) return null;

        var daysLeft = remaining / ratePerDay;
        return daysLeft > 3650 ? null : today.AddDays((int)Math.Ceiling(daysLeft));
    }
}
