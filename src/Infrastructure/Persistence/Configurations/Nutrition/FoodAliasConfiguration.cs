namespace Infrastructure;

public class FoodAliasConfiguration : BaseEntityConfiguration<FoodAlias, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodAlias> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodAliases");

        builder.Property(x => x.Locale).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AliasNormalized).HasMaxLength(256).IsRequired();

        builder.HasIndex(x => new { x.Locale, x.AliasNormalized });
        builder.HasOne<CanonicalFood>().WithMany().HasForeignKey(x => x.CanonicalFoodId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
