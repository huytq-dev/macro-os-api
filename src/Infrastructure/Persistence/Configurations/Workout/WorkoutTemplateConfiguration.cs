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
