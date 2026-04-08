namespace Infrastructure;

public class DailyProgressSnapshotConfiguration
    : BaseEntityConfiguration<DailyProgressSnapshot, Guid>
{
    public override void Configure(EntityTypeBuilder<DailyProgressSnapshot> builder)
    {
        base.Configure(builder);

        builder.ToTable("DailyProgressSnapshots");

        // Nutrition
        builder.Property(x => x.CaloriesConsumed).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.CaloriesTarget).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.ProteinConsumed).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.CarbsConsumed).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.FatConsumed).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.WaterConsumed).HasPrecision(8, 2).IsRequired();

        // Workout
        builder.Property(x => x.WorkoutCount).IsRequired();
        builder.Property(x => x.TotalWorkoutMinutes).IsRequired();
        builder.Property(x => x.CaloriesBurned).HasPrecision(8, 2).IsRequired();

        // Activity
        builder.Property(x => x.StepsCount).IsRequired(false);

        // Body
        builder.Property(x => x.TrendWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.MacroProgramWeekId).IsRequired(false);

        // --- Indexes ---
        // Unique: 1 snapshot per user per day
        builder.HasIndex(x => new { x.UserId, x.SnapshotDate })
            .IsUnique();

        // --- Relationships ---
        builder.HasOne(x => x.MacroProgramWeek)
            .WithMany()
            .HasForeignKey(x => x.MacroProgramWeekId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
