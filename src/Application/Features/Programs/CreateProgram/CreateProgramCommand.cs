namespace Application;

/// <param name="TargetWeightKg">Cân nặng mục tiêu (màn Goal); dùng tính ngày dự kiến.</param>
/// <param name="TrainingType">Loại tập trong program; cập nhật hồ sơ và mức protein g/kg.</param>
/// <param name="CalorieShiftDays">Các thứ ăn thêm khi CalorieShiftMode khác Even; tổng tuần giữ nguyên.</param>
/// <param name="DayTargets">Target từng ngày: đúng 7 phần tử (mỗi thứ một lần). Collaborative: tuỳ chọn (trống = thuật toán tính); Manual: bắt buộc; Coached: không được gửi.</param>
public sealed record CreateProgramCommand(
    GoalType GoalType,
    decimal WeeklyWeightChangeTargetPercent,
    ProgramType ProgramType,
    DietStyle DietStyle,
    ProteinLevel ProteinLevel,
    int? CalorieFloorKcal,
    DayOfWeek? CheckInWeekday,
    int? ManualInitialTdeeKcal,
    decimal? TargetWeightKg = null,
    TrainingType? TrainingType = null,
    CalorieShiftMode? CalorieShiftMode = null,
    List<DayOfWeek>? CalorieShiftDays = null,
    decimal? CalorieShiftPercent = null,
    List<DayTargetInput>? DayTargets = null
) : IRequest<Guid>;
