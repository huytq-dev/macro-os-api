namespace Infrastructure;

public class WorkoutSessionExerciseConfiguration
    : BaseEntityConfiguration<WorkoutSessionExercise, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutSessionExercise> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutSessionExercises");
        builder.Property(x => x.ReplacementReason).HasMaxLength(300).IsRequired(false);
        builder.HasOne<WorkoutExercisePrescription>().WithMany().HasForeignKey(x => x.OriginalPrescriptionId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Exercise>().WithMany().HasForeignKey(x => x.ReplacedExerciseId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.RestSeconds).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.WorkoutSessionId, x.SortOrder });

        // --- Relationships ---
        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.SessionExercises)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Sets)
            .WithOne(x => x.SessionExercise)
            .HasForeignKey(x => x.WorkoutSessionExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
