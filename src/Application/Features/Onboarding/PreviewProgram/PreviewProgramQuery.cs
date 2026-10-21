namespace Application;

/// <summary>
/// Xem trước goal/program ngay trong wizard (ngân sách kcal, ngày dự kiến đạt mục tiêu, macro 7 ngày, sàn kcal).
/// Dùng đúng công thức của CreateProgram nhưng không ghi gì.
/// </summary>
public sealed record PreviewProgramQuery(
    GoalType GoalType,
    decimal? TargetWeightKg,
    decimal WeeklyRatePercent,
    DietStyle DietStyle,
    ProteinLevel ProteinLevel,
    CalorieFloorChoice CalorieFloor,
    TrainingType TrainingType,
    CalorieShiftMode ShiftMode,
    List<DayOfWeek>? ShiftDays,
    decimal? ShiftPercent,
    int? ManualTdeeKcal
) : IRequest<ProgramPreviewResponse>;
