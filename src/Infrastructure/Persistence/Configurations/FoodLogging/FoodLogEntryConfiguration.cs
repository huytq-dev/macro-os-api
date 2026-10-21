namespace Infrastructure;

public class FoodLogEntryConfiguration : SoftDeleteEntityConfiguration<FoodLogEntry, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogEntry> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogEntries");
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).HasDefaultValue(EntrySource.Search).IsRequired();
        builder.Property(x => x.AiConfidence).HasPrecision(5, 4).IsRequired(false);
        builder.Property(x => x.ServingSnapshotJson).HasMaxLength(2000).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<RecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.MealType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EatenAt).IsRequired(false);
        builder.Property(x => x.FoodItemId).IsRequired(false);
        builder.Property(x => x.RecipeId).IsRequired(false);
        builder.Property(x => x.FoodServingId).IsRequired(false);
        builder.Property(x => x.Quantity).HasPrecision(8, 2).IsRequired();

        // Nutrition snapshot
        builder.Property(x => x.Calories).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Protein).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Carbs).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Fat).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Fiber).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.Sodium).HasPrecision(8, 2).IsRequired(false);

        // --- Constraints ---
        // FoodItem XOR Recipe — không được cả hai hoặc không có cái nào
        // Khi log Recipe, FoodServingId phải NULL (serving thuộc FoodItem, không áp dụng cho Recipe)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_FoodLogEntries_FoodOrRecipe",
            "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR " +
            "([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)"
        ));

        // --- Indexes ---
        builder.HasIndex(x => new { x.FoodLogId, x.MealType });

        // --- Relationships ---
        builder.HasOne(x => x.FoodItem)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.Recipe)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.FoodServing)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.FoodServingId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
