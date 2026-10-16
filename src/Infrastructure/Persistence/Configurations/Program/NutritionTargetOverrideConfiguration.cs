namespace Infrastructure;

public class NutritionTargetOverrideConfiguration : BaseEntityConfiguration<NutritionTargetOverride, Guid>
{
    public override void Configure(EntityTypeBuilder<NutritionTargetOverride> builder)
    {
        base.Configure(builder);

        builder.ToTable("NutritionTargetOverrides", t => t.HasCheckConstraint(
            "CK_NutritionTargetOverrides_NonNegative",
            "[CaloriesKcal] >= 0 AND [ProteinG] >= 0 AND [CarbsG] >= 0 AND [FatG] >= 0"));

        builder.Property(x => x.ProteinG).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.CarbsG).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.FatG).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired(false);

        // 1 override / user / ngày
        builder.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
