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

public class RecalculationJobConfiguration : BaseEntityConfiguration<RecalculationJob, Guid>
{
    public override void Configure(EntityTypeBuilder<RecalculationJob> builder)
    {
        base.Configure(builder);

        builder.ToTable("RecalculationJobs");

        builder.Property(x => x.Cause).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000).IsRequired(false);

        builder.HasIndex(x => new { x.State, x.CreatedAt });
        // Idempotent: không xếp 2 job Pending trùng khoảng ngày cho cùng user
        builder.HasIndex(x => new { x.UserId, x.FromDate, x.ToDate }).IsUnique().HasFilter("[State] = 'Pending'");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NutritionDayAggregateConfiguration : BaseEntityConfiguration<NutritionDayAggregate, Guid>
{
    public override void Configure(EntityTypeBuilder<NutritionDayAggregate> builder)
    {
        base.Configure(builder);

        builder.ToTable("NutritionDayAggregates");

        builder.Property(x => x.Calories).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Protein).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Carbs).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Fat).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Fiber).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.NutrientsJson).IsRequired(false);
        builder.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
