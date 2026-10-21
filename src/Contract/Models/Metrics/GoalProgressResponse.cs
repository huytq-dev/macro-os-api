namespace Contract;

public sealed record GoalProgressResponse(
    string GoalType,
    DateOnly StartDate,
    decimal? StartWeightKg,
    decimal? TargetWeightKg,
    decimal? CurrentTrendKg,
    decimal? ProgressPercent,
    decimal? DeviationKg,
    DateOnly? EstimatedCompletionDate,
    IReadOnlyList<GoalWeekModel> Weeks
);
