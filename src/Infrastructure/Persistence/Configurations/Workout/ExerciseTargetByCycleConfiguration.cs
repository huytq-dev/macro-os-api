namespace Infrastructure;

public class ExerciseTargetByCycleConfiguration : BaseEntityConfiguration<ExerciseTargetByCycle, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseTargetByCycle> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseTargetsByCycle", t => t.HasCheckConstraint(
            "CK_ExerciseTargetsByCycle_Ranges",
            "[CycleIndex] >= 1 AND [TargetSets] >= 0 AND [RepMin] >= 0 AND [RepMin] <= [RepMax] " +
            "AND ([Rir] IS NULL OR [Rir] >= 0) AND ([LoadTargetKg] IS NULL OR [LoadTargetKg] >= 0)"));

        builder.Property(x => x.Rir).HasPrecision(3, 1).IsRequired(false);
        builder.Property(x => x.TargetRpe).HasPrecision(3, 1).IsRequired(false);
        builder.Property(x => x.LoadTargetKg).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.SetTypeTemplateJson).HasMaxLength(1000).IsRequired(false);

        builder.HasIndex(x => new { x.WorkoutExercisePrescriptionId, x.CycleIndex }).IsUnique();
    }
}
