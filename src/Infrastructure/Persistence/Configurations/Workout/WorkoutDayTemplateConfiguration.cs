namespace Infrastructure;

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
