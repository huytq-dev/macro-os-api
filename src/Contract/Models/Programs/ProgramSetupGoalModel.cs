namespace Contract;

/// <summary>Goal gần nhất của user để wizard điền sẵn (đang hoạt động, hoặc của program đã kết thúc gần nhất). <paramref name="WeeklyRatePercent"/> là độ lớn không dấu (% cân nặng/tuần).</summary>
public sealed record ProgramSetupGoalModel(string GoalType, decimal WeeklyRatePercent, decimal? TargetWeightKg);
