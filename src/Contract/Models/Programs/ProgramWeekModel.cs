namespace Contract;

public sealed record ProgramWeekModel(
    Guid Id,
    DateOnly EffectiveFrom,
    int CaloriesTarget,
    decimal ProteinTarget,
    decimal CarbsTarget,
    decimal FatTarget,
    string AdjustmentReason,
    int? PreviousCaloriesTarget
);
