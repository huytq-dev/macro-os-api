namespace Infrastructure;

public class WeeklyCheckInConfiguration : BaseEntityConfiguration<WeeklyCheckIn, Guid>
{
    public override void Configure(EntityTypeBuilder<WeeklyCheckIn> builder)
    {
        base.Configure(builder);

        builder.ToTable("WeeklyCheckIns");
        builder.HasOne<CoachingRecommendation>().WithMany().HasForeignKey(x => x.CoachingRecommendationId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.TdeeEstimate).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TrendWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.WeightChangeRateKgPerWeek).HasPrecision(5, 3).IsRequired(false);
        builder.Property(x => x.PreviousCaloriesTarget).IsRequired();
        builder.Property(x => x.NewCaloriesTarget).IsRequired();
        builder.Property(x => x.CaloriesDelta).IsRequired();
        builder.Property(x => x.Accepted).HasDefaultValue(true);
        builder.Property(x => x.WeeklyBudgetKcal).IsRequired(false);

        // --- Indexes ---
        // 1 check-in per user per week
        builder.HasIndex(x => new { x.UserId, x.CheckInDate }).IsUnique();

        // --- Relationships ---
        builder.HasOne(x => x.MacroProgramWeek)
            .WithOne(x => x.CheckIn)
            .HasForeignKey<WeeklyCheckIn>(x => x.MacroProgramWeekId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
