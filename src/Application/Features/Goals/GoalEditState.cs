namespace Application;

/// <param name="ProgramRatePercent">Quy ước của MacroProgram: dương = giảm cân, âm = tăng cân, 0 = giữ.</param>
/// <param name="TdeeKcal">null khi chưa có expenditure lẫn TDEE nhập tay.</param>
public sealed record GoalEditState(
    decimal CurrentWeightKg, GoalType GoalType, decimal? GoalTargetKg, DateOnly GoalStartDate,
    ProgramType ProgramType, decimal ProgramRatePercent, int? FloorKcal, int? TdeeKcal);
