namespace Infrastructure;

public class CoachingRecommendationConfiguration : BaseEntityConfiguration<CoachingRecommendation, Guid>
{
    public override void Configure(EntityTypeBuilder<CoachingRecommendation> builder)
    {
        base.Configure(builder);

        builder.ToTable("CoachingRecommendations");
        builder.HasOne<MacroProgramWeek>().WithMany().HasForeignKey(x => x.OldMacroProgramWeekId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProposedTargetsJson).IsRequired();
        builder.Property(x => x.ReasonCodesJson).IsRequired();
        builder.Property(x => x.Confidence).HasPrecision(5, 4).IsRequired(false);
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).IsRequired();

        // Engine chạy lại cùng kỳ không sinh trùng
        builder.HasIndex(x => new { x.MacroProgramId, x.PeriodStart, x.Type, x.AlgorithmVersion }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        // Restrict: tránh multiple cascade path User → MacroPrograms → CoachingRecommendations
        builder.HasOne(x => x.MacroProgram).WithMany().HasForeignKey(x => x.MacroProgramId).OnDelete(DeleteBehavior.Restrict);
    }
}
