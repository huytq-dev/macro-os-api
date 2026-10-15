namespace Application;

public sealed record CreateProgramCommand(
    GoalType GoalType,
    decimal WeeklyWeightChangeTargetPercent,
    ProgramType ProgramType,
    DietStyle DietStyle,
    ProteinLevel ProteinLevel,
    int? CalorieFloorKcal,
    DayOfWeek? CheckInWeekday,
    int? ManualInitialTdeeKcal
) : IRequest<Guid>;
