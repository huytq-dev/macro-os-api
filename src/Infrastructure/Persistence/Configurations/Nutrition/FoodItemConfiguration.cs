namespace Infrastructure;

public class FoodItemConfiguration : SoftDeleteEntityConfiguration<FoodItem, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodItems");

        // --- Properties ---
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Brand)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(x => x.Barcode)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1024)
            .IsRequired(false);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsVerified).HasDefaultValue(false);

        // Macros per 100g — required
        builder.Property(x => x.CaloriesPer100g).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.ProteinPer100g).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.CarbsPer100g).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.FatPer100g).HasPrecision(8, 2).IsRequired();

        // Micros — optional
        builder.Property(x => x.FiberPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.SugarPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.SodiumPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.PotassiumPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.CalciumPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.IronPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.VitaminCPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.VitaminDPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.SaturatedFatPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.TransFatPer100g).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.CholesterolPer100g).HasPrecision(8, 2).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => x.Barcode);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.CreatedByUserId);

        // --- Relationships ---
        builder.HasMany(x => x.Servings)
            .WithOne(x => x.FoodItem)
            .HasForeignKey(x => x.FoodItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RecipeIngredients)
            .WithOne(x => x.FoodItem)
            .HasForeignKey(x => x.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FoodServingConfiguration : BaseEntityConfiguration<FoodServing, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodServing> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodServings");

        builder.Property(x => x.ServingName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.WeightInGrams)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .HasDefaultValue(false);

        // Chỉ 1 serving default per food item (filter, không chặn nhiều serving thường)
        builder.HasIndex(x => new { x.FoodItemId, x.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");
    }
}