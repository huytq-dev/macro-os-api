namespace Contract;

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
