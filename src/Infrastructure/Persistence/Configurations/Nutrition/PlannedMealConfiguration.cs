namespace Infrastructure;

public class PlannedMealConfiguration : BaseEntityConfiguration<PlannedMeal, Guid>
{
    public override void Configure(EntityTypeBuilder<PlannedMeal> builder)
    {
        base.Configure(builder);

        builder.ToTable("PlannedMeals", t => t.HasCheckConstraint(
            "CK_PlannedMeals_FoodOrRecipe",
            "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR " +
            "([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)"));

        builder.Property(x => x.MealType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(8, 2).IsRequired();

        builder.HasIndex(x => new { x.MealPlanId, x.PlannedDate });

        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne(x => x.FoodServing).WithMany().HasForeignKey(x => x.FoodServingId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<FoodLogEntry>().WithMany().HasForeignKey(x => x.LoggedFoodLogEntryId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
