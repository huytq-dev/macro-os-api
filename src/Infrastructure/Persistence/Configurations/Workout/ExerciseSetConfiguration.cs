namespace Infrastructure;

public class ExerciseSetConfiguration : BaseEntityConfiguration<ExerciseSet, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseSet> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseSets");
        builder.Property(x => x.TargetSnapshotJson).HasMaxLength(1000).IsRequired(false);
        builder.Property(x => x.IsSkipped).HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.WorkoutSessionExerciseId, x.ClientSetId }).IsUnique().HasFilter("[ClientSetId] IS NOT NULL");

        builder.Property(x => x.SetNumber).IsRequired();

        builder.Property(x => x.SetType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Reps).IsRequired(false);
        builder.Property(x => x.WeightKg).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.DurationSeconds).IsRequired(false);
        builder.Property(x => x.DistanceKm).HasPrecision(8, 3).IsRequired(false);
        builder.Property(x => x.Rpm).IsRequired(false);
        builder.Property(x => x.IsCompleted).HasDefaultValue(false);
        builder.Property(x => x.Rir).HasPrecision(4, 1).IsRequired(false);
        builder.Property(x => x.Rpe).IsRequired(false);

        // --- Constraints ---
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExerciseSets_Rpe",
            "[Rpe] IS NULL OR ([Rpe] >= 1 AND [Rpe] <= 10)"
        ));

        // --- Indexes ---
        builder.HasIndex(x => new { x.WorkoutSessionExerciseId, x.SetNumber });
    }
}
