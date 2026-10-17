namespace Infrastructure;

public class DashboardWidgetSettingConfiguration : BaseEntityConfiguration<DashboardWidgetSetting, Guid>
{
    public override void Configure(EntityTypeBuilder<DashboardWidgetSetting> builder)
    {
        base.Configure(builder);

        builder.ToTable("DashboardWidgetSettings", t => t.HasCheckConstraint(
            "CK_DashboardWidgetSettings_SortOrder", "[SortOrder] >= 0"));

        builder.Property(x => x.WidgetKey).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.WidgetKey }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
