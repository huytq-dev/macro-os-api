namespace Domain;

public class UserProfile : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public UnitSystem UnitPreference { get; set; } = UnitSystem.Metric;
    public decimal? HeightCm { get; set; }
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;
    public TrainingType TrainingType { get; set; } = TrainingType.None;
    public string? Bio { get; set; }
    public string? TimeZone { get; set; }
    public string? Locale { get; set; }
    // Onboarding & ưu tiên (master prompt §4.1)
    public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Monday;
    public EnergyUnit EnergyUnit { get; set; } = EnergyUnit.Kcal;
    public TrainingExperience? TrainingExperience { get; set; }
    public NutritionLoggingMode LoggingMode { get; set; } = NutritionLoggingMode.Full;
    public string? DietaryPreferencesJson { get; set; }     // user tự chọn, không dùng để chẩn đoán
    public string? AllergiesJson { get; set; }
    public string? TypicalMealTimesJson { get; set; }
    public OnboardingState OnboardingState { get; set; } = OnboardingState.NotStarted;
    public int OnboardingStep { get; set; }
    // Đáp án wizard Basics (dùng ước tính TDEE khởi điểm; sửa được sau)
    public WeightHistoryAnswer? WeighedMoreThanBefore { get; set; }
    public WeightTrendAnswer? RecentWeightTrend { get; set; }
    public BodyFatBand? BodyFat { get; set; }
    public ExerciseSessionsBand? ExerciseSessions { get; set; }
    public DailyActivityBand? DailyActivity { get; set; }
    public ExperienceLevel? LiftingExperience { get; set; }
    public ExperienceLevel? CardioExperience { get; set; }
    public string? ReferralSource { get; set; }
    public string? AffiliateCode { get; set; }
    // Đồng ý điều khoản cuối wizard (bắt buộc cả hai mới hoàn tất)
    public DateTimeOffset? HealthDisclaimerAcceptedAt { get; set; }
    public DateTimeOffset? PrivacyNoticeAcceptedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public User User { get; set; } = null!;
}
