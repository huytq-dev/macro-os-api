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

    /// <summary>Tên các thứ trong cờ (Monday..Sunday theo thứ tự tuần); rỗng khi không chọn ngày. Dùng để trả cấu hình shift đang lưu cho FE.</summary>
    public static List<string> DayNames(DaysOfWeek days)
        => ProgramTargetResolver.WeekOrder.Where(d => days.HasFlag(Flag(d))).Select(d => d.ToString()).ToList();

    /// <summary>Khoảng % dịch cho phép (validator Create/Preview dùng chung).</summary>
    public const decimal MinPercent = 5m, MaxPercent = 30m;

    public static int SelectedCount(DaysOfWeek days) => System.Numerics.BitOperations.PopCount((uint)days);

    /// <summary>Cấu hình có thật sự dịch kcal không: mode khác Even, percent &gt; 0 và chọn 1–6 ngày. Ngược lại <see cref="ForDay"/> trả nguyên tuần.</summary>
    public static bool Applies(CalorieShiftMode mode, decimal? percent, DaysOfWeek days)
        => mode != CalorieShiftMode.Even && percent is > 0 && SelectedCount(days) is >= 1 and <= 6;

    /// <summary>
    /// kcal và carbs CHƯA kẹp của một ngày sau dịch: ngày chọn ×(1+f), ngày còn lại ×(1 − f·s/(7−s)); chênh kcal đổi qua carbs (4 kcal/g).
    /// Công thức duy nhất cho <see cref="ForDay"/> và <see cref="CalorieShiftFeasibility"/>; chỉ gọi khi <see cref="Applies"/>.
    /// </summary>
    public static (int Calories, decimal CarbsG) Shifted(MacroTargets week, decimal percent, DaysOfWeek days, DayOfWeek day)
    {
        var selected = SelectedCount(days);
        var fraction = percent / 100m;
        var factor = days.HasFlag(Flag(day)) ? 1m + fraction : 1m - fraction * selected / (7 - selected);
        var calories = (int)Math.Round(week.Calories * factor);
        return (calories, week.CarbsG + (calories - week.Calories) / 4m);
    }

    /// <summary>Target của một ngày sau dịch. Đầu ra giữ nguyên hành vi cũ (carbs kẹp ≥ 0); chặn tổ hợp không khả thi là việc của <see cref="CalorieShiftFeasibility"/> lúc tạo/xem trước, không phải của hàm đọc này.</summary>
    public static MacroTargets ForDay(MacroTargets week, CalorieShiftMode mode, decimal? percent, DaysOfWeek days, DayOfWeek day)
    {
        if (!Applies(mode, percent, days)) return week;
        var (calories, carbs) = Shifted(week, percent!.Value, days, day);
        return week with { Calories = calories, CarbsG = Math.Round(Math.Max(0m, carbs), 1) };
    }

    /// <summary>Áp shift của program lên target tuần cho đúng thứ của `date` (dùng cho nhật ký/dashboard).</summary>
    public static ProgramWeekModel ForDate(ProgramWeekModel week, CalorieShiftMode mode, decimal? percent, DaysOfWeek days, DateOnly date)
    {
        if (mode == CalorieShiftMode.Even) return week;
        var shifted = ForDay(new MacroTargets(week.CaloriesTarget, week.ProteinTarget, week.CarbsTarget, week.FatTarget), mode, percent, days, date.DayOfWeek);
        return week with { CaloriesTarget = shifted.Calories, CarbsTarget = shifted.CarbsG };
    }
}
