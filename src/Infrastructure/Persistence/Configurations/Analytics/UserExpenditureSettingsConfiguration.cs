namespace Infrastructure;

public class UserExpenditureSettingsConfiguration : BaseEntityConfiguration<UserExpenditureSettings, Guid>
{
    public override void Configure(EntityTypeBuilder<UserExpenditureSettings> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserExpenditureSettings", t =>
        {
            t.HasCheckConstraint("CK_UserExpenditureSettings_ManualTdee",
                "([InitialEstimateMode] = 'Manual' AND [ManualInitialTdeeKcal] IS NOT NULL AND [ManualInitialTdeeKcal] > 0) " +
                "OR (ISNULL([InitialEstimateMode], '') <> 'Manual' AND [ManualInitialTdeeKcal] IS NULL)");
            t.HasCheckConstraint("CK_UserExpenditureSettings_Revision", "[CalculationRevision] >= 0");
        });

        // null = chưa cấu hình: không ép "Formula" lên user đang có TDEE nhập tay ở program cũ
        builder.Property(x => x.InitialEstimateMode).HasConversion<string>().HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.ManualInitialTdeeKcal).IsRequired(false);
        builder.Property(x => x.BmrEquation).HasConversion<string>().HasMaxLength(30).HasDefaultValue(BmrEquationPreference.MacroFactor).IsRequired();
        builder.Property(x => x.BmrSex).HasConversion<string>().HasMaxLength(10).IsRequired(false);
        builder.Property(x => x.DefaultCalculationStartDate).IsRequired(false);
        builder.Property(x => x.CalculationStartDate).IsRequired(false);
        builder.Property(x => x.CalculationRevision).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        // 1 dòng/user: khởi tạo đồng thời an toàn nhờ unique index (bắt DbUpdateException rồi đọc lại)
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User).WithOne().HasForeignKey<UserExpenditureSettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
