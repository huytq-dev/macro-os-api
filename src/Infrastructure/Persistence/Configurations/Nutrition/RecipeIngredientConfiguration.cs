namespace Infrastructure;

public class RecipeIngredientConfiguration : BaseEntityConfiguration<RecipeIngredient, Guid>
{
    public override void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        base.Configure(builder);

        builder.ToTable("RecipeIngredients");
        builder.Property(x => x.GramsNormalized).HasPrecision(10, 2).IsRequired(false);
        builder.Property(x => x.NutrientSnapshotJson).IsRequired(false);

        builder.Property(x => x.Quantity).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(256).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.RecipeId, x.SortOrder });

        // --- Relationships ---
        builder.HasOne(x => x.FoodItem)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FoodServing)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.FoodServingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
