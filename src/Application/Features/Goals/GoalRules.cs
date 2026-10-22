namespace Application;

/// <summary>
/// MỘT nơi duy nhất định nghĩa hướng và dấu tốc độ của từng <see cref="GoalType"/> (Preview, Create, Update, GoalEdit, FullBodyCompare cùng dùng).
/// Mọi loại liệt kê tường minh, không nhánh mặc định: thêm giá trị enum mới mà quên khai báo ở đây sẽ throw thay vì âm thầm bị coi là giảm cân.
/// </summary>
public static class GoalRules
{
    /// <summary>
    /// Hướng cân nặng của loại goal. WeightLoss = Loss; WeightGain, MuscleGain = Gain; Maintenance = Maintain.
    /// Endurance, Flexibility: pending user decision — giữ nguyên hành vi cũ của Edit Goal (Maintain), trong khi <see cref="SignedRate"/> vẫn cho dấu giảm (xem ghi chú ở đó).
    /// </summary>
    public static GoalDirection DirectionOf(GoalType type) => type switch
    {
        GoalType.WeightLoss => GoalDirection.Loss,
        GoalType.WeightGain => GoalDirection.Gain,
        GoalType.MuscleGain => GoalDirection.Gain,
        GoalType.Maintenance => GoalDirection.Maintain,
        GoalType.Endurance => GoalDirection.Maintain,   // pending user decision
        GoalType.Flexibility => GoalDirection.Maintain, // pending user decision
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>
    /// Tốc độ có dấu theo quy ước <c>MacroProgram.WeeklyWeightChangeTargetPercent</c>: dương = giảm, âm = tăng.
    /// WeightLoss → +|rate|; WeightGain, MuscleGain → −|rate|; Maintenance → 0.
    /// Endurance, Flexibility: pending user decision — giữ nguyên hành vi cũ của Preview/Create/Update (+|rate|, tức giảm), khác với <see cref="DirectionOf"/>.
    /// </summary>
    public static decimal SignedRate(GoalType type, decimal ratePercent) => type switch
    {
        GoalType.WeightLoss => Math.Abs(ratePercent),
        GoalType.WeightGain => -Math.Abs(ratePercent),
        GoalType.MuscleGain => -Math.Abs(ratePercent),
        GoalType.Maintenance => 0m,
        GoalType.Endurance => Math.Abs(ratePercent),   // pending user decision
        GoalType.Flexibility => Math.Abs(ratePercent), // pending user decision
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
