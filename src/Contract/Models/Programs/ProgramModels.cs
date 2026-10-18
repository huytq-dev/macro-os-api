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

public sealed record ProgramResponse(
    Guid Id,
    string ProgramType,
    string GoalType,
    string DietStyle,
    decimal WeeklyWeightChangeTargetPercent,
    decimal WeeklyWeightChangeTargetKg,
    string ProteinLevel,
    decimal ProteinGPerKgBodyweight,
    decimal? FixedProteinGrams,
    int? CalorieFloorKcal,
    string CheckInWeekday,
    DateOnly StartDate,
    DateOnly ExpenditureStartDate,
    int? ManualInitialTdeeKcal,
    bool IsActive,
    ProgramWeekModel? CurrentWeek,
    IReadOnlyList<ProgramWeekModel> Weeks,
    decimal? TargetWeightKg = null,
    int? CheckInDueInDays = null
);
