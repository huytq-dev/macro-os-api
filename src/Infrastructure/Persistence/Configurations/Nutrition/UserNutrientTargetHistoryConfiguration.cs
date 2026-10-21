namespace Infrastructure;

public class UserNutrientTargetHistoryConfiguration : BaseEntityConfiguration<UserNutrientTargetHistory, Guid>
{
    public override void Configure(EntityTypeBuilder<UserNutrientTargetHistory> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserNutrientTargetHistories", t =>
        {
            t.HasCheckConstraint("CK_UserNutrientTargetHistories_Range",
                "([Floor] IS NULL OR [Target] IS NULL OR [Floor] <= [Target])" +
                " AND ([Target] IS NULL OR [Ceiling] IS NULL OR [Target] <= [Ceiling])" +
                " AND ([Floor] IS NULL OR [Ceiling] IS NULL OR [Floor] <= [Ceiling])");
        });

        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.Floor).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.Target).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.Ceiling).HasPrecision(12, 4).IsRequired(false);

        // Một dòng cho mỗi (user, nutrient, ngày hiệu lực)
        builder.HasIndex(x => new { x.UserId, x.NutrientDefinitionId, x.EffectiveFrom }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Nutrient).WithMany().HasForeignKey(x => x.NutrientDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
