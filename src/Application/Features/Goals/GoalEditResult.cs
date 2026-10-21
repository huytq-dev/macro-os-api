namespace Application;

/// <param name="ProgramRatePercent">Giá trị sẽ ghi vào MacroProgram (dương = giảm).</param>
/// <param name="SignedWeeklyKg">Giá trị sẽ ghi vào Goal/Program.WeeklyWeightChangeTargetKg (âm = giảm).</param>
public sealed record GoalEditResult(GoalEditPreviewResponse Preview, GoalType NewGoalType, decimal ProgramRatePercent, decimal SignedWeeklyKg);
