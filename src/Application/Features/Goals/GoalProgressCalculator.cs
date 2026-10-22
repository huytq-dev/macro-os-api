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

    public const string StatusEstimated = "Estimated", StatusGoalAchieved = "GoalAchieved", StatusUnavailable = "Unavailable", StatusNotApplicable = "NotApplicable";
    public const string NoTargetWeight = "NO_TARGET_WEIGHT", RateNotTowardGoal = "RATE_NOT_TOWARD_GOAL", EtaInPast = "ETA_IN_PAST", EtaTooFar = "ETA_TOO_FAR";

    /// <param name="Date">Ngày dự kiến chạm target; CHỈ có khi <c>Status = Estimated</c> và luôn ≥ hôm nay.</param>
    /// <param name="Status">Estimated | GoalAchieved (đã chạm/vượt target theo trend của lần cân cuối: không có ngày giả) | Unavailable (có <c>ReasonCode</c>) | NotApplicable (Maintenance).</param>
    /// <param name="ReasonCode">INSUFFICIENT_WEIGHT_DATA | STALE_WEIGHT_DATA (cùng mã với Weight Trend: cổng TRA / guard G = 14 / dữ liệu cũ) | NO_TARGET_WEIGHT | RATE_NOT_TOWARD_GOAL (tốc độ 0 hoặc đi sai hướng) | ETA_IN_PAST (dự báo từ lần cân cuối đã rơi vào quá khứ mà chưa đạt) | ETA_TOO_FAR (&gt; 10 năm).</param>
    public sealed record EtaResult(DateOnly? Date, string Status, string? ReasonCode);

    /// <summary>
    /// Ngày dự kiến chạm target (MACROOS POLICY, công thức tốc độ CŨ giữ nguyên: ngoại suy tuyến tính hai điểm trong 14 ngày cuối của các lần cân thật, CHƯA đổi sang canonical weekly rate): NEO vào ngày của lần cân thật cuối cùng
    /// và trend tại ngày đó (<c>last.Date + ⌈còn lại ÷ tốc độ⌉</c>), KHÔNG lấy hôm nay làm mốc (trước đây <c>today + ⌈…⌉</c> nên ETA trôi về sau mỗi ngày không cân). Thứ tự: Maintenance ⇒ NotApplicable; không target ⇒ NO_TARGET_WEIGHT;
    /// đã chạm target ⇒ GoalAchieved; cổng TRA / G = 14 / stale không đạt (<paramref name="gateReason"/>) ⇒ Unavailable + mã đó; &lt; 7 ngày dữ liệu ⇒ INSUFFICIENT_WEIGHT_DATA; tốc độ 0 / sai hướng ⇒ RATE_NOT_TOWARD_GOAL;
    /// &gt; 10 năm ⇒ ETA_TOO_FAR; ngày dự báo &lt; hôm nay mà chưa đạt ⇒ ETA_IN_PAST (không bao giờ trả ngày quá khứ như dự báo tương lai).
    /// </summary>
    /// <param name="points">Các lần cân thật của goal (ngày + trend kg), tăng dần theo ngày.</param>
    /// <param name="gateReason">Reason của <see cref="WeightTrendSeries.RateGate"/> tại hôm nay (null = qua cổng).</param>
    public static EtaResult Eta(IReadOnlyList<(DateOnly Date, decimal Kg)> points, decimal? target, GoalDirection direction, DateOnly today, string? gateReason)
    {
        if (direction == GoalDirection.Maintain) return new(null, StatusNotApplicable, null);
        if (target is not { } t) return new(null, StatusUnavailable, NoTargetWeight);
        if (points.Count == 0) return new(null, StatusUnavailable, WeightTrendSeries.InsufficientWeightData);

        var last = points[^1];
        if (direction == GoalDirection.Loss ? last.Kg <= t : last.Kg >= t) return new(null, StatusGoalAchieved, null);
        if (gateReason is not null) return new(null, StatusUnavailable, gateReason);

        var first = points.First(x => x.Date >= last.Date.AddDays(-14));
        var days = last.Date.DayNumber - first.Date.DayNumber;
        if (days < 7) return new(null, StatusUnavailable, WeightTrendSeries.InsufficientWeightData);

        var ratePerDay = (last.Kg - first.Kg) / days;
        var remaining = t - last.Kg;
        if (ratePerDay == 0 || Math.Sign(ratePerDay) != Math.Sign(remaining)) return new(null, StatusUnavailable, RateNotTowardGoal);

        var daysLeft = remaining / ratePerDay;
        if (daysLeft > 3650) return new(null, StatusUnavailable, EtaTooFar);
        var eta = last.Date.AddDays((int)Math.Ceiling(daysLeft));
        return eta < today ? new(null, StatusUnavailable, EtaInPast) : new(eta, StatusEstimated, null);
    }
}
