namespace Application;

public enum CalorieFloorChoice { Standard, Low, Off }

/// <summary>Đầu vào xem trước/tạo program từ wizard (cùng một công thức với CreateProgram để số liệu khớp).</summary>
public sealed record ProgramPreviewInput(
    GoalType GoalType,
    decimal? TargetWeightKg,
    decimal WeeklyRatePercent,
    DietStyle DietStyle,
    ProteinLevel ProteinLevel,
    CalorieFloorChoice CalorieFloor,
    TrainingType TrainingType,
    CalorieShiftMode ShiftMode,
    IReadOnlyList<DayOfWeek>? ShiftDays,
    decimal? ShiftPercent,
    int? ManualTdeeKcal
);

/// <summary>Tính ngân sách kcal, ngày dự kiến và macro 7 ngày từ hồ sơ + cân nặng hiện tại. Hàm thuần, không I/O.</summary>
public static class ProgramPreviewCalculator
{
    private static readonly DayOfWeek[] Week =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    /// <summary>Dương = giảm, âm = tăng (cùng quy ước MacroProgram.WeeklyWeightChangeTargetPercent).</summary>
    public static decimal SignedRate(GoalType goal, decimal ratePercent) => goal switch
    {
        GoalType.Maintenance => 0m,
        GoalType.WeightGain or GoalType.MuscleGain => -Math.Abs(ratePercent),
        _ => Math.Abs(ratePercent),
    };

    public static (int Standard, int Low) Floors(int bmrKcal)
        => (Math.Max(1200, bmrKcal), Math.Max(800, (int)Math.Round(bmrKcal * 0.65m)));

    public static ProgramPreviewResponse Compute(UserProfile profile, decimal weightKg, ProgramPreviewInput input, int kcalPerKg, DateOnly today)
    {
        var estimate = OnboardingCalculator.EstimateTdee(profile, weightKg, today);
        var tdee = input.ManualTdeeKcal ?? estimate.TdeeKcal;
        var (standard, low) = Floors(estimate.BmrKcal);
        int? floor = input.CalorieFloor switch
        {
            CalorieFloorChoice.Standard => standard,
            CalorieFloorChoice.Low => low,
            _ => null,
        };

        var signedRate = SignedRate(input.GoalType, input.WeeklyRatePercent);
        var calories = MacroTargetCalculator.DailyCalories(tdee, signedRate, weightKg, kcalPerKg, floor);
        var proteinPerKg = MacroTargetCalculator.ProteinGPerKg(input.ProteinLevel, input.TrainingType);
        var baseTargets = MacroTargetCalculator.SplitMacros(calories, weightKg, proteinPerKg, null, input.DietStyle);

        var shiftDays = CalorieShiftCalculator.ToFlags(input.ShiftDays);
        var days = Week.Select(day =>
        {
            var targets = CalorieShiftCalculator.ForDay(baseTargets, input.ShiftMode, input.ShiftPercent, shiftDays, day);
            return new ProgramPreviewDay(day.ToString(), targets.Calories, targets.ProteinG, targets.CarbsG, targets.FatG);
        }).ToList();

        var weeklyKg = Math.Round(-signedRate / 100m * weightKg, 2);
        DateOnly? end = null;
        if (input.TargetWeightKg is { } target && weeklyKg != 0)
        {
            var delta = target - weightKg;
            if (Math.Sign(delta) == Math.Sign(weeklyKg))
                end = today.AddDays((int)Math.Ceiling(Math.Abs(delta / weeklyKg) * 7m));
        }

        return new ProgramPreviewResponse(
            tdee, calories, proteinPerKg, weeklyKg, Math.Round(-signedRate, 2), end, standard, low, weightKg, days);
    }
}
