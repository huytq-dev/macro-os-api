namespace Application;

/// <summary>
/// MỘT kiểm khả thi (MacroOS feasibility policy, KHÔNG nhận là hành vi MacroFactor) cho mọi target được tạo/ghi: Preview, Create, UpdateGoal, EditGoal, SetManual, Check-in và chia lại 7 ngày.
/// Quy tắc cho từng ngày (hoặc target tuần): (1) kcal &gt; 0; (2) kcal ≥ sàn áp dụng (chỉ Coached có sàn); (3) macro không âm và 4·P + 4·C + 9·F không vượt kcal quá <see cref="MacroEnergyToleranceKcal"/>.
/// KHÔNG kẹp kcal khi không có sàn, KHÔNG ép carbs = 0, KHÔNG tự hạ protein/fat: vi phạm được từ chối (HTTP 400) hoặc cảnh báo (Preview) cùng mã lý do + cách chỉnh. Hàm thuần.
/// <para>
/// Dung sai làm tròn <see cref="MacroEnergyToleranceKcal"/> = 2 kcal: protein/carbs/fat được lưu làm tròn 0.1 g nên sai số năng lượng tối đa 4·0.05 + 4·0.05 + 9·0.05 = 0.85 kcal;
/// shift thêm tối đa 0.2 kcal do làm tròn carbs. Phần vượt hơn 2 kcal là vượt ngân sách thật, không phải làm tròn. Ngân sách tuần dùng dung sai
/// <see cref="ProgramDayTargets.RoundingTolerance"/> (3 kcal) đã có của chia lại 7 ngày.
/// </para>
/// </summary>
public static class TargetFeasibility
{
    public const string InvalidCalorieTarget = "INVALID_CALORIE_TARGET", MacroBudgetExceeded = "MACRO_BUDGET_EXCEEDED", InfeasibleProgram = "INFEASIBLE_PROGRAM";
    public const string ShiftInfeasible = CalorieShiftFeasibility.ReasonCode;

    public const string NonPositiveCalories = "NON_POSITIVE_CALORIES", BelowFloor = "BELOW_FLOOR", MacroExceedsCalories = "MACRO_EXCEEDS_CALORIES",
        NegativeMacro = "NEGATIVE_MACRO", WeeklyBudgetNotConserved = "WEEKLY_BUDGET_NOT_CONSERVED";

    public const string AdjustGoalRate = "ADJUST_GOAL_RATE", AdjustCalories = "ADJUST_CALORIES", AdjustMacroPreferences = "ADJUST_MACRO_PREFERENCES",
        ReduceShift = "REDUCE_SHIFT", UseEvenDistribution = "USE_EVEN_DISTRIBUTION", UnlockDays = "UNLOCK_DAYS";

    public const decimal MacroEnergyToleranceKcal = 2m;

    public const string MacroKcalMismatch = "MACRO_KCAL_MISMATCH";

    public static FeasibilityResponse Feasible() => new(true, null, [], [], [], null, null, null, [], [], [], null, []);

    /// <summary>Vi phạm của một ngày/tuần: kcal ≤ 0, dưới sàn, macro âm hoặc macro vượt kcal. <paramref name="checkMacros"/> = false cho target user tự nhập (Manual/Collaborative): macro lệch kcal chỉ là cảnh báo như MacroFactor.</summary>
    public static List<FeasibilityViolationModel> Check(string? day, int calories, decimal proteinG, decimal carbsG, decimal fatG, int? floorKcal, bool checkMacros = true)
    {
        var violations = new List<FeasibilityViolationModel>();
        var exact = 4m * proteinG + 4m * carbsG + 9m * fatG;
        var macroKcal = (int)Math.Round(exact, MidpointRounding.AwayFromZero);
        FeasibilityViolationModel V(string code, string detail, int? min = null) => new(code, detail, day, calories, proteinG, carbsG, fatG, macroKcal, min, floorKcal);

        if (calories <= 0) { violations.Add(V(InvalidCalorieTarget, NonPositiveCalories)); return violations; }
        if (floorKcal is { } floor && calories < floor) violations.Add(V(InvalidCalorieTarget, BelowFloor));
        if (!checkMacros) return violations;

        if (proteinG < 0 || carbsG < 0 || fatG < 0) violations.Add(V(MacroBudgetExceeded, NegativeMacro));
        else if (exact - calories > MacroEnergyToleranceKcal) violations.Add(V(MacroBudgetExceeded, MacroExceedsCalories, (int)Math.Ceiling(exact)));
        return violations;
    }

    /// <summary>Target tuần (đã tính, trước shift) + cấu hình shift: tuần không hợp lệ ⇒ INFEASIBLE_PROGRAM (không có shift/chia đều nào cứu được); shift không hợp lệ ⇒ gợi ý % lớn nhất còn hợp lệ, hoặc chia đều tạm thời.</summary>
    public static FeasibilityResponse Assess(MacroTargets week, CalorieShiftMode mode, decimal? percent, DaysOfWeek days, int? shiftFloorKcal)
    {
        var applies = CalorieShiftCalculator.Applies(mode, percent, days);
        var even = EvenDays(week);

        var baseViolations = Check(null, week.Calories, week.ProteinG, week.CarbsG, week.FatG, null);
        if (baseViolations.Count > 0)
            return Infeasible(baseViolations, [], applies ? percent : null, null, null, even, [], withInfeasibleProgram: true);

        if (!applies) return Feasible();
        var shiftViolations = CalorieShiftFeasibility.Validate(week, mode, percent, days, shiftFloorKcal);
        if (shiftViolations.Count == 0) return Feasible();

        var violations = shiftViolations.Select(v => v.Code switch
        {
            CalorieShiftFeasibility.NonPositiveCalories => new FeasibilityViolationModel(InvalidCalorieTarget, NonPositiveCalories, v.Day, v.Calories, week.ProteinG, Math.Round(v.CarbsG, 1), week.FatG, MacroKcalOf(week.ProteinG, v.CarbsG, week.FatG), null, shiftFloorKcal),
            CalorieShiftFeasibility.BelowFloor => new FeasibilityViolationModel(InvalidCalorieTarget, BelowFloor, v.Day, v.Calories, week.ProteinG, Math.Round(v.CarbsG, 1), week.FatG, MacroKcalOf(week.ProteinG, v.CarbsG, week.FatG), null, shiftFloorKcal),
            _ => new FeasibilityViolationModel(MacroBudgetExceeded, NegativeMacro, v.Day, v.Calories, week.ProteinG, Math.Round(v.CarbsG, 1), week.FatG, MacroKcalOf(week.ProteinG, v.CarbsG, week.FatG), null, shiftFloorKcal),
        }).ToList();

        var current = ProgramTargetResolver.WeekOrder.Select(d => ShiftedDay(week, percent!.Value, days, d)).ToList();
        var largest = CalorieShiftFeasibility.LargestValidPercent(week, mode, percent!.Value, days, shiftFloorKcal);
        if (largest is { } pct)
        {
            var proposed = ProgramTargetResolver.WeekOrder.Select(d => ShiftedDay(week, pct, days, d)).ToList();
            return Infeasible(violations, [ShiftInfeasible], percent, pct, ReduceShift, current, proposed, withInfeasibleProgram: false, actions: [ReduceShift, UseEvenDistribution]);
        }
        // Không có % hợp lệ trong mức cho phép: chia đều (đã hợp lệ vì tuần hợp lệ) chỉ được đề xuất, user phải xác nhận
        return Infeasible(violations, [ShiftInfeasible], percent, null, UseEvenDistribution, current, even, withInfeasibleProgram: false, actions: [UseEvenDistribution]);
    }

    /// <summary>Kế hoạch 7 ngày của program có target theo ngày (hoặc sau chia lại ngân sách tuần): chỉ kiểm các ngày bị chia lại + bảo toàn ngân sách tuần (dung sai làm tròn đã có).</summary>
    public static FeasibilityResponse AssessRows(IReadOnlyList<ProgramDayTargets.PlannedDay> plan, int weeklyBudgetKcal, int? floorKcal)
    {
        var violations = new List<FeasibilityViolationModel>();
        foreach (var d in plan.Where(x => x.Changed))
            violations.AddRange(Check(d.Row.DayOfWeek.ToString(), d.Calories, d.ProteinG, d.CarbsG, d.FatG, floorKcal));

        var planned = plan.Sum(x => x.Calories);
        if (Math.Abs(planned - weeklyBudgetKcal) > ProgramDayTargets.RoundingTolerance)
            violations.Add(new FeasibilityViolationModel(InfeasibleProgram, WeeklyBudgetNotConserved, null, planned, 0, 0, 0, weeklyBudgetKcal, null, floorKcal));
        if (violations.Count == 0) return Feasible();

        var days = plan.OrderBy(x => Array.IndexOf(ProgramTargetResolver.WeekOrder, x.Row.DayOfWeek))
            .Select(x => new ProgramDayModel(x.Row.DayOfWeek.ToString(), x.Calories, x.ProteinG, x.CarbsG, x.FatG, x.Row.IsLocked)).ToList();
        return Infeasible(violations, [], null, null, null, days, [], withInfeasibleProgram: true);
    }

    /// <summary>Target 7 ngày do user tự nhập (Collaborative/Manual): chỉ bắt buộc kcal &gt; 0 cho ngày không nhịn ăn; macro lệch kcal vẫn là cảnh báo (MacroFactor cho nhập, ProgramDraftCalculator.Preview).</summary>
    public static FeasibilityResponse AssessUserDays(IEnumerable<DayTargetInput> days)
    {
        var all = days.OrderBy(d => Array.IndexOf(ProgramTargetResolver.WeekOrder, d.DayOfWeek)).ToList();
        var violations = all.Where(d => !d.IsFastingDay).SelectMany(d => Check(d.DayOfWeek.ToString(), d.Calories, d.ProteinG, d.CarbsG, d.FatG, null, checkMacros: false)).ToList();
        // Cảnh báo KHÔNG chặn: macro lệch kcal quá dung sai (cả hai chiều), chỉ ngày hợp lệ (kcal > 0, không nhịn ăn). Không đổi giá trị nào của user
        var warnings = all.Where(d => !d.IsFastingDay && d.Calories > 0).Select(d => MismatchWarning(d.DayOfWeek.ToString(), d.Calories, d.ProteinG, d.CarbsG, d.FatG)).OfType<FeasibilityWarningModel>().ToList();
        if (violations.Count == 0) return Feasible() with { Warnings = warnings };
        var models = all.Select(d => new ProgramDayModel(d.DayOfWeek.ToString(), d.IsFastingDay ? 0 : d.Calories, d.ProteinG, d.CarbsG, d.FatG, d.IsLocked)).ToList();
        return Infeasible(violations, [], null, null, null, models, [], withInfeasibleProgram: false) with { Warnings = warnings };
    }

    /// <summary>Cảnh báo macro ≠ kcal của một ngày do user nhập: |4P+4C+9F − kcal| &gt; <see cref="MacroEnergyToleranceKcal"/> (cả hai chiều); null nếu trong dung sai.</summary>
    public static FeasibilityWarningModel? MismatchWarning(string? day, int calories, decimal proteinG, decimal carbsG, decimal fatG)
    {
        var macroKcal = (int)Math.Round(4m * proteinG + 4m * carbsG + 9m * fatG, MidpointRounding.AwayFromZero);
        var diff = macroKcal - calories;
        if (Math.Abs(4m * proteinG + 4m * carbsG + 9m * fatG - calories) <= MacroEnergyToleranceKcal) return null;
        return new FeasibilityWarningModel(MacroKcalMismatch, day, calories, macroKcal, diff, MacroEnergyToleranceKcal, proteinG, carbsG, fatG, ValidationMessages.MacroKcalMismatch(day, calories, macroKcal, diff));
    }

    /// <summary>Ném HTTP 400 (data = <see cref="FeasibilityResponse"/>, errors.targets = từng vi phạm) nếu không khả thi; ngược lại không làm gì. Gọi TRƯỚC mọi thay đổi để không ghi dở.</summary>
    public static void EnsureFeasible(FeasibilityResponse result)
    {
        if (result.IsFeasible) return;
        var errors = new Dictionary<string, string[]> { ["targets"] = result.Violations.Select(Describe).ToArray() };
        throw new BadRequestException(result.Message ?? ValidationMessages.TargetInfeasible, errors, result);
    }

    public static string Describe(FeasibilityViolationModel v) => v.Detail switch
    {
        NonPositiveCalories => ValidationMessages.TargetNonPositive(v.Day, v.Calories),
        BelowFloor => ValidationMessages.TargetBelowFloor(v.Day, v.Calories, v.FloorKcal ?? 0),
        MacroExceedsCalories => ValidationMessages.TargetMacroExceeds(v.Day, v.Calories, v.MacroKcal, v.MinFeasibleCalories ?? v.MacroKcal),
        WeeklyBudgetNotConserved => ValidationMessages.WeeklyBudgetNotConserved(v.MacroKcal, v.Calories),
        _ => ValidationMessages.TargetNegativeMacro(v.Day),
    };

    private static int MacroKcalOf(decimal p, decimal c, decimal f) => (int)Math.Round(4m * p + 4m * c + 9m * f, MidpointRounding.AwayFromZero);

    private static ProgramDayModel ShiftedDay(MacroTargets week, decimal percent, DaysOfWeek days, DayOfWeek day)
    {
        var (calories, carbs) = CalorieShiftCalculator.Shifted(week, percent, days, day);
        return new ProgramDayModel(day.ToString(), calories, week.ProteinG, Math.Round(carbs, 1), week.FatG, false);
    }

    private static List<ProgramDayModel> EvenDays(MacroTargets week)
        => ProgramTargetResolver.WeekOrder.Select(d => new ProgramDayModel(d.ToString(), week.Calories, week.ProteinG, week.CarbsG, week.FatG, false)).ToList();

    private static FeasibilityResponse Infeasible(
        List<FeasibilityViolationModel> violations, IEnumerable<string> extraCodes, decimal? currentShift, decimal? suggestedShift, string? suggestedAction,
        IReadOnlyList<ProgramDayModel> current, IReadOnlyList<ProgramDayModel> proposed, bool withInfeasibleProgram, IReadOnlyList<string>? actions = null)
    {
        var codes = extraCodes.Concat(violations.Select(v => v.Code)).Distinct().ToList();
        if (withInfeasibleProgram && !codes.Contains(InfeasibleProgram)) codes.Add(InfeasibleProgram);
        var primary = codes[0];

        // Shift: chỉ các lựa chọn shift (user xác nhận). Còn lại: cách chỉnh theo vi phạm (KHÔNG bao giờ gợi ý tắt sàn hay hạ protein/fat tự động)
        var offered = actions?.ToList() ?? [];
        if (actions is null)
            foreach (var v in violations)
                foreach (var a in v.Detail switch
                {
                    NonPositiveCalories => new[] { AdjustGoalRate, AdjustCalories },
                    BelowFloor => new[] { AdjustGoalRate },
                    WeeklyBudgetNotConserved => new[] { UnlockDays, AdjustCalories },
                    _ => new[] { AdjustCalories, AdjustGoalRate, AdjustMacroPreferences },
                })
                    if (!offered.Contains(a)) offered.Add(a);
        var action = suggestedAction ?? (violations.Any(v => v.Detail == WeeklyBudgetNotConserved) ? UnlockDays : withInfeasibleProgram ? AdjustGoalRate : null);

        var affected = violations.Where(v => v.Day is not null).Select(v => v.Day!).Distinct().ToList();
        var message = suggestedAction switch
        {
            ReduceShift => ValidationMessages.ShiftReduceSuggestion(string.Join(", ", affected), suggestedShift ?? 0m),
            UseEvenDistribution => ValidationMessages.ShiftEvenSuggestion(string.Join(", ", affected)),
            _ => violations.Select(Describe).First(),
        };
        return new FeasibilityResponse(false, primary, codes, violations, affected, currentShift, suggestedShift, action, offered, current, proposed, message, []);
    }
}
