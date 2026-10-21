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
