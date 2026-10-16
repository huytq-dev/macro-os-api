namespace Infrastructure;

public class WorkoutTemplateConfiguration : SoftDeleteEntityConfiguration<WorkoutTemplate, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutTemplate> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutTemplates");

        // --- Properties ---
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.Difficulty)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.WorkoutGoal)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EstimatedDurationMinutes).IsRequired();
        builder.Property(x => x.IsPublic).HasDefaultValue(false);
        builder.Property(x => x.IsSystemTemplate).HasDefaultValue(false);
        builder.Property(x => x.CreatedByUserId).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => x.IsPublic);
        builder.HasIndex(x => x.IsSystemTemplate);
        builder.HasIndex(x => x.WorkoutGoal);

        // --- Relationships ---
        builder.HasMany(x => x.Exercises)
            .WithOne(x => x.Template)
            .HasForeignKey(x => x.WorkoutTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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