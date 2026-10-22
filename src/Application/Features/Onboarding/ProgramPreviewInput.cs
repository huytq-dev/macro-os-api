namespace Application;

/// <summary>Đầu vào xem trước/tạo program từ wizard (cùng một công thức với CreateProgram để số liệu khớp).</summary>
public sealed record ProgramPreviewInput(
    GoalType GoalType,
    decimal? TargetWeightKg,
    decimal WeeklyRatePercent,
    DietStyle DietStyle,
    ProteinLevel ProteinLevel,
    CalorieFloorChoice CalorieFloor,
    TrainingType TrainingType,
    CalorieShiftMode ShiftMode,
    IReadOnlyList<DayOfWeek>? ShiftDays,
    decimal? ShiftPercent,
    int? ManualTdeeKcal,
    ProgramType ProgramType = ProgramType.Coached   // wizard onboarding mặc định Coached; floor chỉ áp cho Coached (như Create)
);
