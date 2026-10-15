namespace Contract;

/// <param name="RateActualKgPerWeek">Δtrend 7 ngày (dương = giảm, cùng chiều rate mục tiêu).</param>
public sealed record CheckInWeekSummary(
    decimal AvgCaloriesIn,
    int DaysLogged,
    decimal? TrendStartKg,
    decimal? TrendNowKg,
    decimal? RateActualKgPerWeek,
    decimal RateTargetKgPerWeek,
    decimal TdeeKcal,
    string ExpenditureStatus
);

public sealed record CheckInProposalModel(
    int PreviousCalories,
    int NewCalories,
    int Delta,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    bool Clamped,
    bool FloorApplied,
    string Reason,
    int? WeeklyBudgetKcal
);

public sealed record CheckInPreviewResponse(
    DateOnly NextCheckInDate,
    bool IsDue,
    bool AlreadyCheckedInToday,
    CheckInWeekSummary Week,
    CheckInProposalModel Proposal
);

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
