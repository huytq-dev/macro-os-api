namespace Infrastructure;

public class UserNutrientTargetConfiguration : BaseEntityConfiguration<UserNutrientTarget, Guid>
{
    public override void Configure(EntityTypeBuilder<UserNutrientTarget> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserNutrientTargets", t =>
        {
            t.HasCheckConstraint("CK_UserNutrientTargets_PinOrder", "[PinOrder] IS NULL OR [PinOrder] BETWEEN 1 AND 8");
            t.HasCheckConstraint("CK_UserNutrientTargets_Range",
                "([Floor] IS NULL OR [Target] IS NULL OR [Floor] <= [Target])" +
                " AND ([Target] IS NULL OR [Ceiling] IS NULL OR [Target] <= [Ceiling])" +
                " AND ([Floor] IS NULL OR [Ceiling] IS NULL OR [Floor] <= [Ceiling])");
        });

        builder.Property(x => x.Floor).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.Target).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.Ceiling).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.PinOrder).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.NutrientDefinitionId }).IsUnique();
        // Mỗi vị trí pin dùng một lần/user (tối đa 8 vị trí nhờ check ở trên)
        builder.HasIndex(x => new { x.UserId, x.PinOrder }).IsUnique().HasFilter("[PinOrder] IS NOT NULL");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Nutrient).WithMany().HasForeignKey(x => x.NutrientDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
