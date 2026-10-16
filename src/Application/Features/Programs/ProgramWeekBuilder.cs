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
            week = new MacroProgramWeek { Id = Guid.NewGuid(), MacroProgramId = program.Id, EffectiveFrom = effectiveFrom, RevisionNo = (program.Weeks.Max(w => (int?)w.RevisionNo) ?? 0) + 1 };
            if (previous is not null) previous.EffectiveTo = effectiveFrom.AddDays(-1);
            program.Weeks.Add(week);
        }

        week.CaloriesTarget = targets.Calories;
        week.ProteinTarget = targets.ProteinG;
        week.CarbsTarget = targets.CarbsG;
        week.FatTarget = targets.FatG;
        week.AdjustmentReason = reason;
        week.PreviousCaloriesTarget = previous?.CaloriesTarget;
    }
}
