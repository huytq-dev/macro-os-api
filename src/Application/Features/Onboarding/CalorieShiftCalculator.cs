namespace Application;

/// <summary>
/// "Shift Calorie Targets": các ngày được chọn ăn thêm `percent`%, các ngày còn lại giảm bù để TỔNG TUẦN giữ nguyên.
/// Protein không đổi; chênh kcal chuyển vào carbs (4 kcal/g). Áp lúc đọc nên vẫn đúng sau mỗi check-in. Hàm thuần.
/// </summary>
public static class CalorieShiftCalculator
{
    public static DaysOfWeek Flag(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => DaysOfWeek.Monday,
        DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
        DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
        DayOfWeek.Thursday => DaysOfWeek.Thursday,
        DayOfWeek.Friday => DaysOfWeek.Friday,
        DayOfWeek.Saturday => DaysOfWeek.Saturday,
        _ => DaysOfWeek.Sunday,
    };

    public static DaysOfWeek ToFlags(IEnumerable<DayOfWeek>? days)
        => (days ?? []).Aggregate(DaysOfWeek.None, (flags, day) => flags | Flag(day));

    public static MacroTargets ForDay(MacroTargets week, CalorieShiftMode mode, decimal? percent, DaysOfWeek days, DayOfWeek day)
    {
        if (mode == CalorieShiftMode.Even || percent is not > 0) return week;
        var selected = System.Numerics.BitOperations.PopCount((uint)days);
        if (selected is 0 or >= 7) return week;

        var fraction = percent.Value / 100m;
        var factor = days.HasFlag(Flag(day)) ? 1m + fraction : 1m - fraction * selected / (7 - selected);
        var calories = (int)Math.Round(week.Calories * factor);
        var carbs = Math.Max(0m, week.CarbsG + (calories - week.Calories) / 4m);
        return week with { Calories = calories, CarbsG = Math.Round(carbs, 1) };
    }

    /// <summary>Áp shift của program lên target tuần cho đúng thứ của `date` (dùng cho nhật ký/dashboard).</summary>
    public static ProgramWeekModel ForDate(ProgramWeekModel week, CalorieShiftMode mode, decimal? percent, DaysOfWeek days, DateOnly date)
    {
        if (mode == CalorieShiftMode.Even) return week;
        var shifted = ForDay(new MacroTargets(week.CaloriesTarget, week.ProteinTarget, week.CarbsTarget, week.FatTarget), mode, percent, days, date.DayOfWeek);
        return week with { CaloriesTarget = shifted.Calories, CarbsTarget = shifted.CarbsG };
    }
}
