namespace Contract;

public sealed record CheckInHistoryModel(
    Guid Id,
    DateOnly CheckInDate,
    decimal TdeeEstimate,
    decimal? TrendWeightKg,
    decimal? WeightChangeRateKgPerWeek,
    int PreviousCaloriesTarget,
    int NewCaloriesTarget,
    int CaloriesDelta,
    bool Accepted,
    int? WeeklyBudgetKcal
);
