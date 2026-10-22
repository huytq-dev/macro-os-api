namespace Application;

/// <summary>
/// Hệ số hoạt động kiểu MacroFactor: cơ sở theo mức hoạt động hằng ngày (1.2 / 1.4 / 1.6) + phần cộng theo số buổi tập/tuần (0 / 0.1 / 0.2 / 0.3); TDEE khởi điểm = BMR đã điều chỉnh × (cơ sở + cộng).
/// Bảng hệ số: đã xác minh. Ánh xạ bước/ngày → mức hoạt động (&lt;5k / 5–15k / &gt;15k) CHƯA xác minh (quyết định 11 còn mở), caller phải truyền mức đã chọn.
/// Chưa nối vào luồng nào; <see cref="OnboardingCalculator.EstimateTdee"/> vẫn dùng bảng cũ.
/// </summary>
public static class ActivityMultipliers
{
    public static decimal Base(DailyActivityBand daily) => daily switch
    {
        DailyActivityBand.MostlySedentary => 1.2m,
        DailyActivityBand.ModeratelyActive => 1.4m,
        _ => 1.6m,
    };

    public static decimal Exercise(ExerciseSessionsBand sessions) => sessions switch
    {
        ExerciseSessionsBand.None => 0m,
        ExerciseSessionsBand.OneToThree => 0.1m,
        ExerciseSessionsBand.FourToSix => 0.2m,
        _ => 0.3m,
    };

    public static decimal Total(DailyActivityBand daily, ExerciseSessionsBand sessions) => Base(daily) + Exercise(sessions);
}
