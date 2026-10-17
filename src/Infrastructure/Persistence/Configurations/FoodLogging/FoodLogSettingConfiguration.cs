namespace Infrastructure;

public class FoodLogSettingConfiguration : BaseEntityConfiguration<FoodLogSetting, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogSetting> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogSettings", t => t.HasCheckConstraint(
            "CK_FoodLogSettings_HourRange",
            "[HourRangeFirst] BETWEEN 0 AND 23 AND [HourRangeLast] BETWEEN 0 AND 23 AND [HourRangeLast] - [HourRangeFirst] >= 1"));

        builder.Property(x => x.Overages).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Alignment).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.AddFoodsToHour).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.WeekBanner).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CalendarScrolling).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Optimization).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TimelineTileDensity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TimelineHighlight).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.LoggerBannerDisplay).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.LoggerTileDensity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.LoggerHighlight).HasConversion<string>().HasMaxLength(20).IsRequired();

        // 1 dòng/user
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
