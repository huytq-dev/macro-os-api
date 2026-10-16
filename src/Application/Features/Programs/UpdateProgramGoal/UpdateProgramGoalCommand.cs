namespace Application;

/// <summary>Đổi goal/rate/diet/protein/floor/ngày check-in của program đang active; null = giữ nguyên. Sinh lại target tuần hiện tại (Manual).</summary>
public sealed record UpdateProgramGoalCommand(
    GoalType? GoalType,
    decimal? WeeklyWeightChangeTargetPercent,
    DietStyle? DietStyle,
    ProteinLevel? ProteinLevel,
    decimal? FixedProteinGrams,
    int? CalorieFloorKcal,
    DayOfWeek? CheckInWeekday
) : IRequest<Unit>;
