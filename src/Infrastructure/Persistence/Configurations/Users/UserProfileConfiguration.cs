namespace Infrastructure;

public class UserProfileConfiguration : BaseEntityConfiguration<UserProfile, Guid>
{
    public override void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserProfiles");
        builder.Property(x => x.WeekStartDay).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.EnergyUnit).HasConversion<string>().HasMaxLength(10).HasDefaultValue(EnergyUnit.Kcal).IsRequired();
        builder.Property(x => x.TrainingExperience).HasConversion<string>().HasMaxLength(30).IsRequired(false);
        builder.Property(x => x.LoggingMode).HasConversion<string>().HasMaxLength(30).HasDefaultValue(NutritionLoggingMode.Full).IsRequired();
        builder.Property(x => x.DietaryPreferencesJson).HasMaxLength(2000).IsRequired(false);
        builder.Property(x => x.AllergiesJson).HasMaxLength(2000).IsRequired(false);
        builder.Property(x => x.TypicalMealTimesJson).HasMaxLength(1000).IsRequired(false);
        builder.Property(x => x.OnboardingState).HasConversion<string>().HasMaxLength(30).HasDefaultValue(OnboardingState.NotStarted).IsRequired();
        builder.Property(x => x.OnboardingStep).HasDefaultValue(0);
        builder.Property(x => x.WeighedMoreThanBefore).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.RecentWeightTrend).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.BodyFat).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.ExerciseSessions).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.DailyActivity).HasConversion<string>().HasMaxLength(30).IsRequired(false);
        builder.Property(x => x.LiftingExperience).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.CardioExperience).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.ReferralSource).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.AffiliateCode).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.CheckInIntroSeen).HasDefaultValue(false);
        builder.Property(x => x.PartialLoggingModuleHidden).HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // --- Properties ---
        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DateOfBirth)
            .IsRequired(false);

        builder.Property(x => x.Bio)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.TimeZone)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Locale)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.UnitPreference)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(UnitSystem.Metric);

        builder.Property(x => x.ActivityLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TrainingType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TrainingType.None);

        builder.Property(x => x.HeightCm)
            .HasPrecision(5, 2)
            .IsRequired(false);
    }
}