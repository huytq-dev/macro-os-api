namespace Infrastructure;

public class AiIngredientCacheConfiguration : BaseEntityConfiguration<AiIngredientCache, Guid>
{
    public override void Configure(EntityTypeBuilder<AiIngredientCache> builder)
    {
        base.Configure(builder);

        builder.ToTable("AiIngredientCaches");

        builder.Property(x => x.NormalizedName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Locale).HasMaxLength(10).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Locale, x.NormalizedName }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CanonicalFood>().WithMany().HasForeignKey(x => x.CanonicalFoodId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<FoodItem>().WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
