namespace Infrastructure;

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
