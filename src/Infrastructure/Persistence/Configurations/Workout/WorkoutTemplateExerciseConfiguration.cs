namespace Infrastructure;

public class WorkoutTemplateExerciseConfiguration
    : BaseEntityConfiguration<WorkoutTemplateExercise, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutTemplateExercise> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutTemplateExercises");

        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.DefaultSets).IsRequired();
        builder.Property(x => x.DefaultReps).IsRequired(false);
        builder.Property(x => x.DefaultWeight).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.DefaultDurationSeconds).IsRequired(false);
        builder.Property(x => x.DefaultDistance).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.RestSeconds).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.WorkoutTemplateId, x.SortOrder });

        // --- Relationships ---
        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.TemplateExercises)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
