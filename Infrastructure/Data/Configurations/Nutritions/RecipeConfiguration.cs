namespace Infrastructure;

public class RecipeConfiguration : SoftDeleteEntityConfiguration<Recipe, Guid>
{
    public override void Configure(EntityTypeBuilder<Recipe> builder)
    {
        base.Configure(builder);

        builder.ToTable("Recipes");

        // --- Properties ---
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1024)
            .IsRequired(false);

        builder.Property(x => x.ServingsCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.PrepTimeMinutes).IsRequired(false);
        builder.Property(x => x.CookTimeMinutes).IsRequired(false);
        builder.Property(x => x.IsPublic).HasDefaultValue(false);

        builder.Property(x => x.SourceUrl)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder.Property(x => x.MealType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Denormalized totals
        builder.Property(x => x.TotalCalories).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalProtein).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalCarbs).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalFat).HasPrecision(8, 2).IsRequired();

        // --- Indexes ---
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.IsPublic);

        // --- Relationships ---
        builder.HasMany(x => x.Ingredients)
            .WithOne(x => x.Recipe)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RecipeIngredientConfiguration : BaseEntityConfiguration<RecipeIngredient, Guid>
{
    public override void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        base.Configure(builder);

        builder.ToTable("RecipeIngredients");

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