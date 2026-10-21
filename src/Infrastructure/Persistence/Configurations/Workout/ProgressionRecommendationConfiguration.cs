namespace Infrastructure;

public class ProgressionRecommendationConfiguration : BaseEntityConfiguration<ProgressionRecommendation, Guid>
{
    public override void Configure(EntityTypeBuilder<ProgressionRecommendation> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProgressionRecommendations");

        builder.Property(x => x.SuggestedWeightKg).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReasonCodesJson).IsRequired();
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Confidence).HasPrecision(5, 4).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.ExerciseId, x.CreatedAt });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReferenceSession).WithMany().HasForeignKey(x => x.ReferenceSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutExercisePrescription>().WithMany().HasForeignKey(x => x.WorkoutExercisePrescriptionId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
