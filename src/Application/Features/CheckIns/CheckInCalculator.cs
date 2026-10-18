namespace Application;

/// <param name="RateTargetKg">kg/tuần mục tiêu tại cân hiện tại (dương = giảm).</param>
public sealed record CheckInProposal(
    int PreviousCalories,
    int RawCalories,
    int NewCalories,
    int Delta,
    bool Clamped,
    bool FloorApplied,
    decimal RateTargetKg,
    MacroTargets Targets
);

/// <summary>ALG-04 (thuần): target tuần mới từ TDEE[t], rate mục tiêu theo trend hiện tại, hedge ±Δmax, sàn kcal, protein giữ nguyên trừ khi cân đổi > 2 kg (ADR-005).</summary>
public static class CheckInCalculator
{
    public static CheckInProposal Propose(MacroProgram p, decimal trendNow, decimal tdee, MacroProgramWeek prev, AlgorithmOptions opt)
    {
        var rateTargetKg = Math.Round(p.WeeklyWeightChangeTargetPercent / 100m * trendNow, 3);
        var raw = (int)Math.Round(tdee - rateTargetKg * opt.KcalPerKg / 7m);

        var delta = Math.Clamp(raw - prev.CaloriesTarget, -opt.MaxWeeklyDeltaKcal, opt.MaxWeeklyDeltaKcal);
        var clamped = delta != raw - prev.CaloriesTarget;
        var kcal = prev.CaloriesTarget + delta;

        var floorApplied = false;
        if (p.ProgramType == ProgramType.Coached && p.CalorieFloorKcal is { } floor && kcal < floor)
        {
            kcal = floor;
            floorApplied = true;
        }

        // ADR-005: protein giữ nguyên trừ khi cân (suy từ protein tuần trước) lệch > 2 kg
        var gPerKg = p.ProteinGPerKgBodyweight;
        var impliedPrevKg = gPerKg > 0 ? prev.ProteinTarget / gPerKg : trendNow;
        var protein = p.FixedProteinGrams ?? (Math.Abs(impliedPrevKg - trendNow) > 2m ? Math.Round(gPerKg * trendNow, 1) : prev.ProteinTarget);

        var targets = MacroTargetCalculator.SplitMacros(kcal, trendNow, gPerKg, protein, p.DietStyle);
        return new CheckInProposal(prev.CaloriesTarget, raw, kcal, kcal - prev.CaloriesTarget, clamped, floorApplied, rateTargetKg, targets);
    }

    /// <summary>Ngày check-in kế tiếp: lần đầu = weekday đầu tiên sau StartDate; sau đó = weekday đầu tiên sau check-in cuối. Due khi today ≥ ngày đó − 2 (sớm ≤ 2 ngày).</summary>
    public static (DateOnly NextDate, bool IsDue) Next(DateOnly today, DayOfWeek weekday, DateOnly startDate, DateOnly? lastCheckIn)
    {
        var after = lastCheckIn ?? startDate;
        var next = after.AddDays(1);
        while (next.DayOfWeek != weekday) next = next.AddDays(1);
        return (next, today >= next.AddDays(-2));
    }

    /// <summary>Số ngày từ hôm nay tới ngày check-in kế tiếp; quá hạn (đã qua ngày đó mà chưa check-in) tính là 0.</summary>
    public static int DaysUntil(DateOnly today, DateOnly nextCheckIn) => Math.Max(0, nextCheckIn.DayNumber - today.DayNumber);

    public static string Reason(CheckInProposal pr, ExpenditureStatus status, decimal? rateActual, decimal rateTarget)
    {
        if (status == ExpenditureStatus.Calibrating) return "Đang hiệu chỉnh expenditure: thay đổi nhỏ cho tới khi đủ dữ liệu.";
        var parts = new List<string>();
        if (rateActual is { } ra)
            parts.Add(ra > rateTarget ? $"Giảm nhanh hơn mục tiêu ({ra:0.00} vs {rateTarget:0.00} kg/tuần) → tăng kcal"
                    : ra < rateTarget ? $"Giảm chậm hơn mục tiêu ({ra:0.00} vs {rateTarget:0.00} kg/tuần) → giảm kcal"
                    : "Đúng tốc độ mục tiêu");
        if (pr.Clamped) parts.Add("Giới hạn thay đổi tối đa mỗi tuần");
        if (pr.FloorApplied) parts.Add("Chạm sàn kcal");
        if (status == ExpenditureStatus.Holding) parts.Add("Expenditure đang Holding: dùng TDEE cuối");
        return parts.Count == 0 ? "Giữ nguyên target" : string.Join("; ", parts);
    }
}
