namespace Infrastructure;

public class MealTemplateItemConfiguration : BaseEntityConfiguration<MealTemplateItem, Guid>
{
    public override void Configure(EntityTypeBuilder<MealTemplateItem> builder)
    {
        base.Configure(builder);

        // FoodItem XOR Recipe (Recipe thì FoodServingId phải NULL) — như CK_FoodLogEntries_FoodOrRecipe
        builder.ToTable("MealTemplateItems", t => t.HasCheckConstraint(
            "CK_MealTemplateItems_FoodOrRecipe",
            "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR " +
            "([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)"));

        builder.Property(x => x.Quantity).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.MealType).HasConversion<string>().HasMaxLength(50).IsRequired(false);

        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne(x => x.FoodServing).WithMany().HasForeignKey(x => x.FoodServingId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
