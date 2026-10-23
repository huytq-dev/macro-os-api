namespace Infrastructure;

public class GoalConfiguration : BaseEntityConfiguration<Goal, Guid>
{
    public override void Configure(EntityTypeBuilder<Goal> builder)
    {
        base.Configure(builder);

        builder.ToTable("Goals", t => t.HasCheckConstraint("CK_Goals_TrackingMode", "[TrackingMode] IN (N'Trend', N'Scale')"));

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.TrackingMode).HasConversion<string>().HasMaxLength(10).HasDefaultValue(GoalTrackingMode.Trend).IsRequired();
        builder.Property(x => x.StartScaleWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.TargetWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.StartWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RatePerWeekKg).HasPrecision(4, 2).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
