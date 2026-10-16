namespace Contract;

/// <summary>Trạng thái wizard thiết lập + đáp án đã lưu để mở lại đúng bước.</summary>
/// <param name="IsRequired">Chưa có program đang chạy hoặc chưa đồng ý điều khoản: app chuyển thẳng vào wizard.</param>
public sealed record OnboardingStateResponse(
    string State,
    int Step,
    bool IsRequired,
    bool HasProgram,
    bool ConsentAccepted,
    string? Gender,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    decimal? WeightKg,
    string UnitPreference,
    string? WeighedMoreThanBefore,
    string? RecentWeightTrend,
    string? BodyFat,
    string? ExerciseSessions,
    string? DailyActivity,
    string? LiftingExperience,
    string? CardioExperience
);

/// <summary>TDEE khởi điểm ước tính từ câu trả lời Basics.</summary>
/// <param name="Method">MifflinStJeor, hoặc KatchMcArdle khi đã chọn tỉ lệ mỡ.</param>
public sealed record TdeeEstimateResponse(int TdeeKcal, int BmrKcal, string Method, decimal WeightKg);

public sealed record ProgramPreviewDay(string DayOfWeek, int Calories, decimal ProteinG, decimal CarbsG, decimal FatG);

/// <summary>Xem trước goal/program trước khi tạo: ngân sách kcal, ngày dự kiến đạt mục tiêu, macro từng ngày.</summary>
/// <param name="WeeklyChangeKg">Có dấu: dương = tăng cân, âm = giảm.</param>
/// <param name="ProjectedEndDate">Ước lượng lạc quan; null khi giữ cân, chưa chọn cân mục tiêu hoặc mục tiêu ngược hướng.</param>
public sealed record ProgramPreviewResponse(
    int TdeeKcal,
    int AverageCalories,
    decimal ProteinGPerKg,
    decimal WeeklyChangeKg,
    decimal WeeklyChangePercent,
    DateOnly? ProjectedEndDate,
    int FloorStandardKcal,
    int FloorLowKcal,
    decimal CurrentWeightKg,
    IReadOnlyList<ProgramPreviewDay> Days
);
