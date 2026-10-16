namespace Infrastructure;

public class RecipeVersionConfiguration : BaseEntityConfiguration<RecipeVersion, Guid>
{
    public override void Configure(EntityTypeBuilder<RecipeVersion> builder)
    {
        base.Configure(builder);

        builder.ToTable("RecipeVersions");

        builder.Property(x => x.Servings).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.CookedYieldGrams).HasPrecision(10, 2).IsRequired(false);
        builder.Property(x => x.RawTotalGrams).HasPrecision(10, 2).IsRequired(false);
        builder.Property(x => x.Instructions).HasMaxLength(4000).IsRequired(false);
        builder.Property(x => x.IngredientsSnapshotJson).IsRequired();
        builder.Property(x => x.TotalCalories).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.TotalProtein).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.TotalCarbs).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.TotalFat).HasPrecision(10, 2).IsRequired();

        builder.HasIndex(x => new { x.RecipeId, x.VersionNo }).IsUnique();

        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PreparedRecipeBatchConfiguration : BaseEntityConfiguration<PreparedRecipeBatch, Guid>
{
    public override void Configure(EntityTypeBuilder<PreparedRecipeBatch> builder)
    {
        base.Configure(builder);

        builder.ToTable("PreparedRecipeBatches", t => t.HasCheckConstraint(
            "CK_PreparedRecipeBatches_YieldPositive", "[ActualYieldGrams] > 0"));

        builder.Property(x => x.ActualYieldGrams).HasPrecision(10, 2).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PreparedAt });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RecipeVersion).WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MealTemplateConfiguration : SoftDeleteEntityConfiguration<MealTemplate, Guid>
{
    public override void Configure(EntityTypeBuilder<MealTemplate> builder)
    {
        base.Configure(builder);

        builder.ToTable("MealTemplates");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Name });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Items).WithOne(x => x.MealTemplate).HasForeignKey(x => x.MealTemplateId).OnDelete(DeleteBehavior.Cascade);
    }
}

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
