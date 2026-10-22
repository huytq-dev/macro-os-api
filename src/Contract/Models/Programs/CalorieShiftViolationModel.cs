namespace Contract;

/// <summary>Một ngày vi phạm khi dịch kcal (400 <see cref="CalorieShiftInfeasibleModel"/>).</summary>
/// <param name="Day">Tên thứ (Monday…Sunday).</param>
/// <param name="Code">NON_POSITIVE_CALORIES (kcal ≤ 0) | BELOW_FLOOR (dưới sàn kcal) | CARBS_BELOW_ZERO (carbs sau khi dịch &lt; 0, macro sẽ không khớp kcal). Một ngày có thể có nhiều mã.</param>
/// <param name="Calories">kcal của ngày sau khi dịch (có thể ≤ 0).</param>
/// <param name="CarbsG">Carbs của ngày sau khi dịch, CHƯA kẹp về 0 (âm khi mã CARBS_BELOW_ZERO).</param>
public sealed record CalorieShiftViolationModel(string Day, string Code, int Calories, decimal CarbsG);
