namespace Application;

/// <summary>
/// Target hiệu lực của một ngày: dòng MacroProgramDayTarget của đúng thứ (nếu có) thắng, ngày không có dòng thì dùng target tuần + dịch kcal của program.
/// MỌI nơi đọc target (nhật ký, dashboard, snapshot, check-in) phải đi qua đây để không lệch nhau. Hàm thuần, không I/O; cần nạp sẵn program.DayTargets.
/// </summary>
public static class ProgramTargetResolver
{
    public static readonly DayOfWeek[] WeekOrder =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static ProgramWeekModel ForDay(MacroProgramWeek week, MacroProgram program, DayOfWeek day)
    {
        var model = week.Adapt<ProgramWeekModel>();
        var row = program.DayTargets.FirstOrDefault(d => d.DayOfWeek == day);
        if (row is null)
        {
            var shifted = CalorieShiftCalculator.ForDay(
                new MacroTargets(model.CaloriesTarget, model.ProteinTarget, model.CarbsTarget, model.FatTarget),
                program.CalorieShiftMode, program.CalorieShiftPercent, program.CalorieShiftDays, day);
            return model with { CaloriesTarget = shifted.Calories, CarbsTarget = shifted.CarbsG };
        }

        if (row.IsFastingDay) return model with { CaloriesTarget = 0, ProteinTarget = 0, CarbsTarget = 0, FatTarget = 0 };
        return model with
        {
            CaloriesTarget = row.CaloriesTarget ?? model.CaloriesTarget,
            ProteinTarget = row.ProteinTarget ?? model.ProteinTarget,
            CarbsTarget = row.CarbsTarget ?? model.CarbsTarget,
            FatTarget = row.FatTarget ?? model.FatTarget,
        };
    }

    public static ProgramWeekModel ForDate(MacroProgramWeek week, MacroProgram program, DateOnly date) => ForDay(week, program, date.DayOfWeek);

    /// <summary>7 ngày Thứ 2 → Chủ nhật theo tuần <paramref name="week"/> (cho thẻ Program / màn Review).</summary>
    public static List<ProgramDayModel> Days(MacroProgramWeek week, MacroProgram program)
        => WeekOrder.Select(day =>
        {
            var t = ForDay(week, program, day);
            var row = program.DayTargets.FirstOrDefault(d => d.DayOfWeek == day);
            return new ProgramDayModel(day.ToString(), t.CaloriesTarget, t.ProteinTarget, t.CarbsTarget, t.FatTarget, row?.IsLocked ?? false);
        }).ToList();
}
