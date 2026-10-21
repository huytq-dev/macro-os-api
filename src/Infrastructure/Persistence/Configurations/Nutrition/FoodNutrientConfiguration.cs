namespace Infrastructure;

public class FoodNutrientConfiguration : BaseEntityConfiguration<FoodNutrient, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodNutrient> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodNutrients", t => t.HasCheckConstraint(
            "CK_FoodNutrients_AmountNonNegative", "[AmountPer100g] IS NULL OR [AmountPer100g] >= 0"));

        builder.Property(x => x.AmountPer100g).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.AmountStatus).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.FoodItemId, x.NutrientDefinitionId }).IsUnique();

        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Nutrient).WithMany().HasForeignKey(x => x.NutrientDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
