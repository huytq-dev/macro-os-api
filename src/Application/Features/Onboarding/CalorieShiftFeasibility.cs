namespace Application;

/// <summary>
/// Tổ hợp Calorie Shift có tạo ra ngày không hợp lệ không (hàm thuần, cùng công thức với <see cref="CalorieShiftCalculator.Shifted"/>): ngày kcal ≤ 0, dưới sàn kcal (nếu program có sàn),
/// hoặc carbs sau khi dịch &lt; 0 (trước đây bị kẹp về 0 nên macro không còn khớp kcal). Dùng ở MỌI đường ghi cấu hình shift (Create, Review, onboarding Preview);
/// không bao giờ tự đổi % / số ngày / macro của user: chỉ chặn và trả lý do + cách chỉnh tính từ chính công thức.
/// </summary>
public static class CalorieShiftFeasibility
{
    public const string ReasonCode = "CALORIE_SHIFT_INFEASIBLE";
    public const string NonPositiveCalories = "NON_POSITIVE_CALORIES", BelowFloor = "BELOW_FLOOR", CarbsBelowZero = "CARBS_BELOW_ZERO";
    public const string ReducePercent = "REDUCE_PERCENT", ReduceDays = "REDUCE_DAYS", LowerFloor = "LOWER_FLOOR", RemoveFloor = "REMOVE_FLOOR";

    /// <summary>Vi phạm theo từng ngày (rỗng = hợp lệ hoặc không có shift thật sự). <paramref name="week"/> là target tuần TRƯỚC khi dịch; <paramref name="floorKcal"/> null = không có sàn.</summary>
    public static List<CalorieShiftViolationModel> Validate(MacroTargets week, CalorieShiftMode mode, decimal? percent, DaysOfWeek days, int? floorKcal)
    {
        var violations = new List<CalorieShiftViolationModel>();
        if (!CalorieShiftCalculator.Applies(mode, percent, days)) return violations;

        foreach (var day in ProgramTargetResolver.WeekOrder)
        {
            var (calories, carbs) = CalorieShiftCalculator.Shifted(week, percent!.Value, days, day);
            if (calories <= 0) violations.Add(new(day.ToString(), NonPositiveCalories, calories, carbs));
            else if (floorKcal is { } floor && calories < floor) violations.Add(new(day.ToString(), BelowFloor, calories, carbs));
            if (Math.Round(carbs, 1) < 0) violations.Add(new(day.ToString(), CarbsBelowZero, calories, carbs));
        }
        return violations;
    }

    /// <summary>
    /// % shift LỚN NHẤT (bước 0.01) nhỏ hơn % đã chọn mà đúng các ngày đã chọn vẫn hợp lệ, không dưới <see cref="CalorieShiftCalculator.MinPercent"/> (mức tối thiểu cho phép của setting);
    /// null nếu ngay cả mức tối thiểu cũng không hợp lệ (hoặc % đã chọn không lớn hơn mức tối thiểu). Mỗi kết quả được kiểm lại bằng <see cref="Validate"/>, không có mức giảm cố định.
    /// </summary>
    public static decimal? LargestValidPercent(MacroTargets week, CalorieShiftMode mode, decimal percent, DaysOfWeek days, int? floorKcal)
    {
        bool Valid(decimal p) => Validate(week, mode, p, days, floorKcal).Count == 0;
        var lowest = (int)(CalorieShiftCalculator.MinPercent * 100m);
        var chosen = (int)Math.Ceiling(percent * 100m);
        if (lowest >= chosen || !Valid(lowest / 100m)) return null;

        int lo = lowest, hi = chosen - 1; // hợp lệ là tiền tố theo %: ngày còn lại chỉ thấp đi khi % tăng
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (Valid(mid / 100m)) lo = mid; else hi = mid - 1;
        }
        return lo / 100m;
    }

    /// <summary>
    /// Cách chỉnh để hợp lệ, mỗi cách đều được kiểm lại bằng <see cref="Validate"/> (không phải ước lượng): % lớn nhất với đúng các ngày đã chọn (bước 0.01, không dưới
    /// <see cref="CalorieShiftCalculator.MinPercent"/>), số ngày lớn nhất với đúng % đã chọn, và (chỉ khi bỏ sàn là đủ) hạ sàn tới kcal ngày thấp nhất hoặc tắt sàn.
    /// </summary>
    public static List<CalorieShiftOptionModel> Options(MacroTargets week, CalorieShiftMode mode, decimal percent, DaysOfWeek days, int? floorKcal)
    {
        var options = new List<CalorieShiftOptionModel>();
        bool Valid(decimal p, DaysOfWeek d, int? floor) => Validate(week, mode, p, d, floor).Count == 0;

        if (LargestValidPercent(week, mode, percent, days, floorKcal) is { } largest)
            options.Add(new(ReducePercent, MaxPercent: largest));

        var chosenDays = ProgramTargetResolver.WeekOrder.Where(d => days.HasFlag(CalorieShiftCalculator.Flag(d))).ToList();
        for (var n = chosenDays.Count - 1; n >= 1; n--) // hợp lệ chỉ phụ thuộc số ngày chọn, không phụ thuộc ngày nào
        {
            if (!Valid(percent, CalorieShiftCalculator.ToFlags(chosenDays.Take(n)), floorKcal)) continue;
            options.Add(new(ReduceDays, MaxDays: n));
            break;
        }

        if (floorKcal is not null && Valid(percent, days, null))
        {
            var lowestDay = ProgramTargetResolver.WeekOrder.Min(d => CalorieShiftCalculator.Shifted(week, percent, days, d).Calories);
            options.Add(new(LowerFloor, MaxFloorKcal: lowestDay));
            options.Add(new(RemoveFloor));
        }
        return options;
    }
}
