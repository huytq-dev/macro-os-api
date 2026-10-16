namespace Infrastructure;

public class WorkoutProgramConfiguration : SoftDeleteEntityConfiguration<WorkoutProgram, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutProgram> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutPrograms");
        builder.Property(x => x.Notes).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Goal).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreationMode).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.Color).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Tối đa 1 program Active / user (ngoài ra không giới hạn số program lưu)
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[Status] = 'Active' AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // CurrentRevisionId: FK vòng với Revisions, NoAction để tránh cycle
        builder.HasOne<WorkoutProgramRevision>().WithMany().HasForeignKey(x => x.CurrentRevisionId)
            .OnDelete(DeleteBehavior.NoAction).IsRequired(false);
        builder.HasMany(x => x.Revisions).WithOne(x => x.WorkoutProgram).HasForeignKey(x => x.WorkoutProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkoutProgramRevisionConfiguration : BaseEntityConfiguration<WorkoutProgramRevision, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutProgramRevision> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutProgramRevisions", t => t.HasCheckConstraint(
            "CK_WorkoutProgramRevisions_Ranges", "[CycleCount] >= 1 AND [DaysPerCycle] >= 1 AND [RevisionNo] >= 1"));

        builder.Property(x => x.GeneratorVersion).HasMaxLength(30).IsRequired(false);
        builder.Property(x => x.ConfigJson).IsRequired(false);

        builder.HasIndex(x => new { x.WorkoutProgramId, x.RevisionNo }).IsUnique();

        // Restrict: tránh multiple cascade path User → GymProfiles/WorkoutPrograms → Revisions
        builder.HasOne(x => x.GymProfile).WithMany().HasForeignKey(x => x.GymProfileId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasMany(x => x.Days).WithOne(x => x.Revision).HasForeignKey(x => x.WorkoutProgramRevisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkoutDayTemplateConfiguration : BaseEntityConfiguration<WorkoutDayTemplate, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutDayTemplate> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutDayTemplates", t => t.HasCheckConstraint(
            "CK_WorkoutDayTemplates_DayIndex", "[DayIndex] >= 1"));

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.WorkoutProgramRevisionId, x.DayIndex }).IsUnique();

        builder.HasOne(x => x.GymProfile).WithMany().HasForeignKey(x => x.GymProfileId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasMany(x => x.Prescriptions).WithOne(x => x.DayTemplate).HasForeignKey(x => x.WorkoutDayTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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
