namespace Application;

/// <summary>
/// Hiệu chỉnh theo mục tiêu ban đầu của MacroFactor (VERIFIED, macrofactor.com/expenditure-modifiers): <c>AdjustedInitialTDEE = InitialTDEE × (1 + 4 × signedWeeklyRate)</c>,
/// với <c>signedWeeklyRate</c> là tỉ lệ cân nặng/tuần dạng phân số, GIẢM CÂN ÂM (−1%/tuần = −0.01 ⇒ ×0.96; +0.5%/tuần ⇒ ×1.02).
/// KHÁC với Predictive Goal Adjustment (điều chỉnh khi đổi mục tiêu). Hàm thuần, CHƯA nối vào luồng nào và KHÔNG được kích hoạt cho tới khi điều kiện áp dụng rõ + được duyệt:
/// chưa rõ có luôn áp cho ước tính mặc định hay phụ thuộc công tắc Predictive Goal; không áp cho Manual Initial hay TDEE đã học nếu không có bằng chứng; không cộng dồn với ×0.95/0.97/0.92.
/// </summary>
public static class InitialGoalCorrection
{
    public const decimal Coefficient = 4m;

    /// <param name="signedWeeklyRate">Phân số cân nặng/tuần: giảm cân ÂM, tăng cân DƯƠNG.</param>
    public static decimal Factor(decimal signedWeeklyRate) => 1m + Coefficient * signedWeeklyRate;

    /// <summary>Từ quy ước của MacroOS (<c>MacroProgram.WeeklyWeightChangeTargetPercent</c>: dương = giảm, âm = tăng, đơn vị %).</summary>
    public static decimal FactorFromProgramPercent(decimal weeklyWeightChangeTargetPercent) => Factor(-weeklyWeightChangeTargetPercent / 100m);
}
