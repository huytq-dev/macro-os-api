namespace Application;

/// <summary>Dùng chung cho Create/UpdateGoal/SetManualExpenditure: tính target từ program + cân nặng và ghi MacroProgramWeek của hôm nay (upsert theo EffectiveFrom).</summary>
public static class ProgramWeekBuilder
{
    public static MacroTargets Compute(MacroProgram program, decimal weightKg, int tdee, AlgorithmOptions options)
    {
        var calories = MacroTargetCalculator.DailyCalories(
            tdee, program.WeeklyWeightChangeTargetPercent, weightKg, options.KcalPerKg,
            program.ProgramType == ProgramType.Coached ? program.CalorieFloorKcal : null);

        return MacroTargetCalculator.SplitMacros(calories, weightKg, program.ProteinGPerKgBodyweight, program.FixedProteinGrams, program.DietStyle);
    }

    public static void UpsertWeek(MacroProgram program, DateOnly effectiveFrom, MacroTargets targets, AdjustmentReason reason)
    {
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
        ProgramDayTargets.Rebalance(program, week);
    }
}
