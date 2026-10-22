namespace Infrastructure;

public class ExpenditureModePeriodConfiguration : BaseEntityConfiguration<ExpenditureModePeriod, Guid>
{
    public override void Configure(EntityTypeBuilder<ExpenditureModePeriod> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExpenditureModePeriods", t =>
            t.HasCheckConstraint("CK_ExpenditureModePeriods_Static",
                "([Mode] = 'Static' AND [StaticTdeeKcal] IS NOT NULL AND [StaticTdeeKcal] > 0) " +
                "OR ([Mode] = 'Dynamic' AND [StaticTdeeKcal] IS NULL)"));

        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.StaticTdeeKcal).IsRequired(false);

        // Mỗi ngày hiệu lực một dòng ⇒ không có hai khoảng chồng nhau; chuyển lại trong ngày = ghi đè dòng ngày đó
        builder.HasIndex(x => new { x.UserId, x.EffectiveFrom }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
