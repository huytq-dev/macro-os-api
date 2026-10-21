namespace Infrastructure;

public class CheckInDecisionConfiguration : BaseEntityConfiguration<CheckInDecision, Guid>
{
    public override void Configure(EntityTypeBuilder<CheckInDecision> builder)
    {
        base.Configure(builder);

        builder.ToTable("CheckInDecisions");

        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(30).IsRequired();

        // Tối đa 1 quyết định / recommendation
        builder.HasIndex(x => x.CoachingRecommendationId).IsUnique();

        builder.HasOne(x => x.Recommendation).WithMany().HasForeignKey(x => x.CoachingRecommendationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedMacroProgramWeek).WithMany().HasForeignKey(x => x.CreatedMacroProgramWeekId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
