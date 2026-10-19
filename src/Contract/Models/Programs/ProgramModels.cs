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
    int? CheckInDueInDays = null,
    bool CheckInDue = false,
    IReadOnlyList<ProgramDayModel>? Days = null
);

/// <summary>Target hiệu lực của một ngày trong tuần (đã tính DayTarget hoặc dịch kcal); Thứ 2 → Chủ nhật.</summary>
public sealed record ProgramDayModel(string DayOfWeek, int Calories, decimal ProteinG, decimal CarbsG, decimal FatG, bool IsLocked);

public sealed record ProgramFloorsModel(int StandardKcal, int LowKcal);

/// <summary>Mọi số wizard New Program cần để vẽ và chặn: cân nặng, TDEE, sàn kcal, trần kcal/protein, 7 ngày mặc định (duy trì cân, Balanced, protein vừa).</summary>
public sealed record ProgramSetupResponse(
    decimal WeightKg,
    int EstimatedTdeeKcal,
    ProgramFloorsModel Floors,
    int KcalMax,
    decimal ProteinPerKgMax,
    IReadOnlyList<ProgramDayModel> Defaults,
    ProgramSetupGoalModel? Goal,
    string CheckInWeekday
);

/// <summary>Goal gần nhất của user để wizard điền sẵn (đang hoạt động, hoặc của program đã kết thúc gần nhất). <paramref name="WeeklyRatePercent"/> là độ lớn không dấu (% cân nặng/tuần).</summary>
public sealed record ProgramSetupGoalModel(string GoalType, decimal WeeklyRatePercent, decimal? TargetWeightKg);

/// <param name="Code">MACRO_KCAL_MISMATCH (kcal từ macro lệch > 15% kcal đã nhập) | BELOW_FLOOR (kcal ngày thấp hơn sàn đã chọn).</param>
public sealed record ProgramDraftWarning(string Code, string? DayOfWeek);

/// <summary>Xem trước program chưa tạo (không ghi DB). Rate/kg là SUY RA từ kcal trung bình so với TDEE: dương = tăng cân, âm = giảm (cùng quy ước onboarding preview).</summary>
public sealed record ProgramDraftPreviewResponse(
    IReadOnlyList<ProgramDayModel> Days,
    int AverageCalories,
    int WeeklyBudgetKcal,
    decimal AverageProteinG,
    decimal AverageCarbsG,
    decimal AverageFatG,
    int MacroKcalAverage,
    int EstimatedTdeeKcal,
    decimal WeeklyChangeKg,
    decimal WeeklyChangePercent,
    DateOnly? ProjectedEndDate,
    IReadOnlyList<ProgramDraftWarning> Warnings
);
