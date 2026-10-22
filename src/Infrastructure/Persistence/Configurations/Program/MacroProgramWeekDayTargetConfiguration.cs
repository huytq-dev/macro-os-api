namespace Infrastructure;

public class MacroProgramWeekDayTargetConfiguration : BaseEntityConfiguration<MacroProgramWeekDayTarget, Guid>
{
    public override void Configure(EntityTypeBuilder<MacroProgramWeekDayTarget> builder)
    {
        base.Configure(builder);

        builder.ToTable("MacroProgramWeekDayTargets", t => t.HasCheckConstraint("CK_MacroProgramWeekDayTargets_Revision", "[SnapshotRevision] >= 1"));
        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.DayType).HasConversion<string>().HasMaxLength(30).HasDefaultValue(DayType.Standard).IsRequired();
        builder.Property(x => x.Distribution).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.AdjustmentReason).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.AdditionalTargetsJson).HasMaxLength(1000).IsRequired(false);
        builder.Property(x => x.IsFastingDay).HasDefaultValue(false);
        builder.Property(x => x.IsLocked).HasDefaultValue(false);

        builder.Property(x => x.CaloriesTarget).IsRequired();
        builder.Property(x => x.ProteinTarget).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.CarbsTarget).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.FatTarget).HasPrecision(6, 2).IsRequired();

        // Mỗi phiên bản tuần có nhiều bộ snapshot (mỗi lần ghi lại cùng ngày thêm một bộ), mỗi bộ đúng một dòng/ngày trong tuần
        builder.HasIndex(x => new { x.MacroProgramWeekId, x.SnapshotRevision, x.DayOfWeek }).IsUnique();

        builder.HasOne(x => x.MacroProgramWeek)
            .WithMany(x => x.DayTargetSnapshots)
            .HasForeignKey(x => x.MacroProgramWeekId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
