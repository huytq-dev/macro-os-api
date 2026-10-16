namespace Infrastructure;

public class WorkoutSessionConfiguration : SoftDeleteEntityConfiguration<WorkoutSession, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutSessions");
        builder.Property(x => x.PausedSeconds).HasDefaultValue(0);
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        // Dedupe khi sync offline: 1 ClientSessionId / user
        builder.HasIndex(x => new { x.UserId, x.ClientSessionId }).IsUnique().HasFilter("[ClientSessionId] IS NOT NULL");
        builder.HasOne<WorkoutProgram>().WithMany().HasForeignKey(x => x.WorkoutProgramId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(WorkoutSessionStatus.InProgress);

        builder.Property(x => x.WorkoutTemplateId).IsRequired(false);

        // Neo vào program revision / ngày / gym; Restrict vì User → Sessions đã Cascade (tránh multiple cascade path)
        builder.HasOne<WorkoutProgramRevision>().WithMany().HasForeignKey(x => x.WorkoutProgramRevisionId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<WorkoutDayTemplate>().WithMany().HasForeignKey(x => x.WorkoutDayTemplateId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<GymProfile>().WithMany().HasForeignKey(x => x.GymProfileId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.Property(x => x.StartTime).IsRequired(false);
        builder.Property(x => x.EndTime).IsRequired(false);
        builder.Property(x => x.DurationMinutes).IsRequired(false);
        builder.Property(x => x.CaloriesBurned).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.PerceivedExertion).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(1000).IsRequired(false);
        builder.Property(x => x.MoodBefore).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.MoodAfter).HasMaxLength(50).IsRequired(false);

        // --- Constraints ---
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WorkoutSessions_PerceivedExertion",
            "[PerceivedExertion] IS NULL OR ([PerceivedExertion] >= 1 AND [PerceivedExertion] <= 10)"
        ));

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.SessionDate });
        builder.HasIndex(x => new { x.UserId, x.Status });

        // --- Relationships ---
        builder.HasOne(x => x.Template)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.WorkoutTemplateId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasMany(x => x.Exercises)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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