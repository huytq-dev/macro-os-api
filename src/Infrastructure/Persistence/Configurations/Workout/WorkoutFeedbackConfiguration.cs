namespace Infrastructure;

public class WorkoutFeedbackConfiguration : BaseEntityConfiguration<WorkoutFeedback, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutFeedback> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutFeedback", t => t.HasCheckConstraint(
            "CK_WorkoutFeedback_Scale",
            "([PerceivedEffort] IS NULL OR [PerceivedEffort] BETWEEN 1 AND 10) AND ([Pump] IS NULL OR [Pump] BETWEEN 1 AND 10) AND " +
            "([Soreness] IS NULL OR [Soreness] BETWEEN 1 AND 10) AND ([Recovery] IS NULL OR [Recovery] BETWEEN 1 AND 10)"));

        builder.Property(x => x.Comment).HasMaxLength(1000).IsRequired(false);

        builder.HasIndex(x => x.WorkoutSessionId);

        builder.HasOne(x => x.WorkoutSession).WithMany().HasForeignKey(x => x.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        // Restrict: Session → SessionExercises và Session → Feedback đều Cascade
        builder.HasOne(x => x.WorkoutSessionExercise).WithMany().HasForeignKey(x => x.WorkoutSessionExerciseId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
