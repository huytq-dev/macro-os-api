namespace Domain;

public class User : EntityBase<Guid>, ISoftDeletable
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public bool? EmailVerified { get; set; }
    public UserRole Role { get; set; } = UserRole.Vip;
    public string? AvatarUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    //public bool? PhoneVerified { get; set; }
    //public DateTimeOffset? LastLoginAt { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Navigation
    public UserProfile Profile { get; set; } = null!;
    public ICollection<MacroProgram> MacroPrograms { get; set; } = [];
    public ICollection<WeightEntry> WeightEntries { get; set; } = [];
    public ICollection<FoodLog> FoodLogs { get; set; } = [];
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = [];
    public ICollection<BodyMetric> BodyMetrics { get; set; } = [];
    public ICollection<WaterLog> WaterLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<ReminderSetting> ReminderSettings { get; set; } = [];
    public ICollection<UserSubscription> Subscriptions { get; set; } = [];
    public ICollection<DailyProgressSnapshot> DailyProgressSnapshots { get; set; } = [];
    public ICollection<ExpenditureSnapshot> ExpenditureSnapshots { get; set; } = [];
    public ICollection<WeeklyCheckIn> WeeklyCheckIns { get; set; } = [];
    public ICollection<PeriodLog> PeriodLogs { get; set; } = [];
    public ICollection<LoggingStreak> LoggingStreaks { get; set; } = [];
    public ICollection<HealthIntegration> HealthIntegrations { get; set; } = [];
}
