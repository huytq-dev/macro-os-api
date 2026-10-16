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

public class PersonalRecordConfiguration : BaseEntityConfiguration<PersonalRecord, Guid>
{
    public override void Configure(EntityTypeBuilder<PersonalRecord> builder)
    {
        base.Configure(builder);

        builder.ToTable("PersonalRecords");

        builder.Property(x => x.Metric).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Value).HasPrecision(10, 3).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ExerciseId, x.Metric });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseSet>().WithMany().HasForeignKey(x => x.ExerciseSetId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}

public class WorkoutMuscleAggregateConfiguration : BaseEntityConfiguration<WorkoutMuscleAggregate, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutMuscleAggregate> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutMuscleAggregates");

        builder.Property(x => x.MuscleGroup).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.HardSets).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.VolumeKg).HasPrecision(12, 2).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PeriodStart, x.MuscleGroup }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
