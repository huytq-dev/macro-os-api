namespace Infrastructure;

public class WorkoutExercisePrescriptionConfiguration : BaseEntityConfiguration<WorkoutExercisePrescription, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutExercisePrescription> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutExercisePrescriptions", t => t.HasCheckConstraint(
            "CK_WorkoutExercisePrescriptions_Rest", "[RestSeconds] IS NULL OR [RestSeconds] >= 0"));

        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.ProgressionPolicy).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Tempo).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.LoadType).HasConversion<string>().HasMaxLength(30).HasDefaultValue(LoadType.Weighted).IsRequired();
        builder.Property(x => x.MinLoadKg).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.MaxLoadKg).HasPrecision(6, 2).IsRequired(false);

        builder.HasIndex(x => new { x.WorkoutDayTemplateId, x.SortOrder });

        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Targets).WithOne(x => x.Prescription).HasForeignKey(x => x.WorkoutExercisePrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
