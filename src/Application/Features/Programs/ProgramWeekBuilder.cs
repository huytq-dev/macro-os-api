namespace Application;

/// <summary>Dùng chung cho Create/UpdateGoal/SetManualExpenditure: tính target từ program + cân nặng và ghi MacroProgramWeek của hôm nay (upsert theo EffectiveFrom).</summary>
public static class ProgramWeekBuilder
{
    public static MacroTargets Compute(MacroProgram program, decimal weightKg, int tdee, AlgorithmOptions options)
        => ComputeTargets(tdee, program.WeeklyWeightChangeTargetPercent, weightKg, options.KcalPerKg, program.CalorieFloorKcal, program.ProgramType,
            program.ProteinGPerKgBodyweight, program.FixedProteinGrams, program.DietStyle);

    /// <summary>
    /// MỘT công thức kcal + macro cho Preview (onboarding), Create, UpdateGoal, SetManual (hết lệch Preview ≠ Create): sàn calo chỉ áp cho chương trình Coached.
    /// <paramref name="ratePercent"/> theo quy ước MacroProgram (dương = giảm).
    /// </summary>
    public static MacroTargets ComputeTargets(int tdee, decimal ratePercent, decimal weightKg, int kcalPerKg, int? floorKcal, ProgramType programType,
        decimal proteinGPerKg, decimal? fixedProteinG, DietStyle dietStyle)
    {
        var calories = MacroTargetCalculator.DailyCalories(tdee, ratePercent, weightKg, kcalPerKg, programType == ProgramType.Coached ? floorKcal : null);
        return MacroTargetCalculator.SplitMacros(calories, weightKg, proteinGPerKg, fixedProteinG, dietStyle);
    }

    /// <summary>Sàn kcal ÁP DỤNG cho target tuần/ngày: chỉ Coached có sàn (cùng quy tắc <see cref="ComputeTargets"/>).</summary>
    public static int? EffectiveFloor(MacroProgram program) => program.ProgramType == ProgramType.Coached ? program.CalorieFloorKcal : null;

    /// <summary>
    /// Kiểm khả thi (kcal &gt; 0, macro ≤ kcal, shift, 7 ngày) của target tuần MỚI trên cấu hình hiện tại của program, KHÔNG ghi gì. Program có target theo ngày: kiểm kế hoạch chia lại 7 ngày;
    /// còn lại kiểm target tuần + shift (dùng cùng <see cref="TargetFeasibility"/> với Preview/Create/Check-in). Cần nạp sẵn <c>program.DayTargets</c>.
    /// </summary>
    public static FeasibilityResponse Assess(MacroProgram program, MacroTargets targets)
    {
        if (program.DayTargets.Count > 0)
        {
            var week = new MacroProgramWeek { CaloriesTarget = targets.Calories, ProteinTarget = targets.ProteinG, CarbsTarget = targets.CarbsG, FatTarget = targets.FatG };
            return TargetFeasibility.AssessRows(ProgramDayTargets.Plan(program.DayTargets, week), targets.Calories * 7, EffectiveFloor(program));
        }
        return TargetFeasibility.Assess(targets, program.CalorieShiftMode, program.CalorieShiftPercent, program.CalorieShiftDays, program.CalorieFloorKcal);
    }

    /// <param name="validate">true (mặc định): từ chối (400) target không khả thi TRƯỚC mọi thay đổi. false chỉ cho Reject/Accept-không-đề-xuất (sao chép target đã chấp nhận trước đó, không phải target mới).</param>
    public static void UpsertWeek(MacroProgram program, DateOnly effectiveFrom, MacroTargets targets, AdjustmentReason reason, bool validate = true)
    {
        if (validate) TargetFeasibility.EnsureFeasible(Assess(program, targets));
        var previous = program.Weeks.Where(w => w.EffectiveFrom < effectiveFrom).OrderByDescending(w => w.EffectiveFrom).FirstOrDefault();
        var week = program.Weeks.FirstOrDefault(w => w.EffectiveFrom == effectiveFrom);
        if (week is null)
        {
            // Không gán Id: week thêm vào program ĐANG ĐƯỢC TRACK (check-in, đổi goal) mà có sẵn Id thì EF coi là Modified và UPDATE 0 dòng (DbUpdateConcurrencyException); để trống thì EF sinh Id và INSERT
            week = new MacroProgramWeek { MacroProgramId = program.Id, EffectiveFrom = effectiveFrom, RevisionNo = (program.Weeks.Max(w => (int?)w.RevisionNo) ?? 0) + 1 };
            if (previous is not null) previous.EffectiveTo = effectiveFrom.AddDays(-1);
            program.Weeks.Add(week);
        }

        week.CaloriesTarget = targets.Calories;
        week.ProteinTarget = targets.ProteinG;
        week.CarbsTarget = targets.CarbsG;
        week.FatTarget = targets.FatG;
        week.AdjustmentReason = reason;
        week.PreviousCaloriesTarget = previous?.CaloriesTarget;

        // Program có target theo ngày: chia lại 7 ngày cho khớp ngân sách mới (ngày khoá giữ nguyên); target tuần = trung bình thật của 7 ngày. Cần nạp sẵn program.DayTargets
        ProgramDayTargets.Rebalance(program, week, validate);

        // D2: chụp 7 target ngày hiệu lực của phiên bản tuần này (cùng giao dịch SaveChanges); không bao giờ sửa bộ snapshot cũ, ghi lại cùng ngày thêm bộ mới
        WeekDaySnapshots.Capture(program, week, reason);
    }
}
