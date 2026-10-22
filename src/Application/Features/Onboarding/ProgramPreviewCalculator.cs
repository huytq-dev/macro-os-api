namespace Application;

/// <summary>Tính ngân sách kcal, ngày dự kiến và macro 7 ngày từ hồ sơ + cân nặng hiện tại. Hàm thuần, không I/O.</summary>
public static class ProgramPreviewCalculator
{
    private static readonly DayOfWeek[] Week =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    /// <summary>Sàn calo theo quy tắc MacroOS (chưa khớp MF, xem audit C8). Không có BMR (thiếu câu trả lời) ⇒ chỉ còn sàn tối thiểu cố định 1200/800.</summary>
    public static (int Standard, int Low) Floors(int? bmrKcal)
        => bmrKcal is { } bmr ? (Math.Max(1200, bmr), Math.Max(800, (int)Math.Round(bmr * 0.65m))) : (1200, 800);

    public static ProgramPreviewResponse Compute(UserProfile profile, decimal weightKg, ProgramPreviewInput input, int kcalPerKg, DateOnly today, BiologicalSex? bmrSex = null)
    {
        // today = ngày LOCAL của user. input.ManualTdeeKcal là TDEE đã được resolver chọn (đã học > nhập tay > công thức); công thức chỉ cần cho sàn calo
        var estimate = OnboardingCalculator.TryEstimateTdee(profile, weightKg, today, bmrSex, out var missing);
        var tdee = input.ManualTdeeKcal ?? estimate?.TdeeKcal ?? throw new BadRequestException(ExceptionMessages.InitialEstimateNeedsAnswers(string.Join(", ", missing)));
        var (standard, low) = Floors(estimate?.BmrKcal);
        int? floor = input.CalorieFloor switch
        {
            CalorieFloorChoice.Standard => standard,
            CalorieFloorChoice.Low => low,
            _ => null,
        };

        var signedRate = GoalRules.SignedRate(input.GoalType, input.WeeklyRatePercent);
        var proteinPerKg = MacroTargetCalculator.ProteinGPerKg(input.ProteinLevel, input.TrainingType);
        // Cùng hàm với Create/UpdateGoal/SetManual: sàn calo chỉ áp cho Coached
        var baseTargets = ProgramWeekBuilder.ComputeTargets(tdee, signedRate, weightKg, kcalPerKg, floor, input.ProgramType, proteinPerKg, null, input.DietStyle);
        var calories = baseTargets.Calories;

        var shiftDays = CalorieShiftCalculator.ToFlags(input.ShiftDays);
        // Xem trước không ném: trả FeasibilityResponse (cảnh báo + mã lý do + đề xuất shift nhỏ hơn) để user chỉnh trước khi Create; Create vẫn từ chối 400 cùng kiểm này
        var feasibility = TargetFeasibility.Assess(baseTargets, input.ShiftMode, input.ShiftPercent, shiftDays, floor);
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
            tdee, calories, proteinPerKg, weeklyKg, Math.Round(-signedRate, 2), end, standard, low, weightKg, days, feasibility);
    }
}
