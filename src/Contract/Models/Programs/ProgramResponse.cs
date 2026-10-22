namespace Contract;

/// <param name="CalorieShiftMode">Even | TrainingDays | Custom: cấu hình shift ĐANG LƯU (để FE gửi lại đúng khi xác nhận shift nhỏ hơn qua UpdateProgramGoal).</param>
/// <param name="CalorieShiftDays">Các thứ ăn thêm (Monday..Sunday, thứ tự tuần); rỗng khi Even.</param>
/// <param name="CalorieShiftPercent">% shift đang lưu; null khi Even.</param>
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
    int? CheckInDueInDays = null,
    bool CheckInDue = false,
    IReadOnlyList<ProgramDayModel>? Days = null,
    string CalorieShiftMode = "Even",
    IReadOnlyList<string>? CalorieShiftDays = null,
    decimal? CalorieShiftPercent = null
);
