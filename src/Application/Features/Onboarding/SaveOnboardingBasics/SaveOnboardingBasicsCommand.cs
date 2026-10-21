namespace Application;

/// <summary>
/// Lưu đáp án màn Basics từng bước (một phần, trường nào gửi mới cập nhật). `Step` = số bước đã hoàn thành.
/// Có WeightKg thì ghi cân nặng hôm nay (WeightEntry đầu tiên, trend khởi điểm).
/// </summary>
public sealed record SaveOnboardingBasicsCommand(
    int Step,
    Gender? Gender,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    decimal? WeightKg,
    UnitSystem? UnitPreference,
    WeightHistoryAnswer? WeighedMoreThanBefore,
    WeightTrendAnswer? RecentWeightTrend,
    BodyFatBand? BodyFat,
    ExerciseSessionsBand? ExerciseSessions,
    DailyActivityBand? DailyActivity,
    ExperienceLevel? LiftingExperience,
    ExperienceLevel? CardioExperience,
    string? ReferralSource,
    string? AffiliateCode
) : IRequest<Unit>;
