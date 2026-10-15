namespace Contract;

public sealed record ExpenditurePointModel(
    DateOnly SnapshotDate,
    decimal EstimatedTdeeKcal,
    string Status,
    decimal AvgCaloriesConsumed,
    decimal? WeightChangeRateKgPerWeek,
    int WindowDays
);

/// <param name="HoldingReason">Lý do Holding của điểm mới nhất; null nếu không Holding.</param>
public sealed record ExpenditureResponse(
    ExpenditurePointModel? Current,
    string? HoldingReason,
    DateOnly? ExpenditureStartDate,
    IReadOnlyList<ExpenditurePointModel> Points
);

public sealed record DailyProgressModel(
    DateOnly SnapshotDate,
    decimal CaloriesConsumed,
    decimal CaloriesTarget,
    decimal ProteinConsumed,
    decimal CarbsConsumed,
    decimal FatConsumed,
    decimal? TrendWeightKg,
    Guid? MacroProgramWeekId
);
