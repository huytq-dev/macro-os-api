namespace Infrastructure;

public class FoodItemConfiguration : SoftDeleteEntityConfiguration<FoodItem, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodItems");
        builder.Property(x => x.Country).HasMaxLength(2).IsRequired(false);
        builder.Property(x => x.Language).HasMaxLength(10).IsRequired(false);
        builder.Property(x => x.ProviderCode).HasMaxLength(30).IsRequired(false);
        builder.Property(x => x.ExternalFoodId).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.QualityStatus).HasConversion<string>().HasMaxLength(30).HasDefaultValue(FoodQualityStatus.Unverified).IsRequired();
        builder.Property(x => x.OriginalUnits).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.IsUserEdited).HasDefaultValue(false);
        builder.Property(x => x.DensityGPerMl).HasPrecision(8, 4).IsRequired(false);
        builder.Property(x => x.Version);
        builder.Property(x => x.IsActive);
        builder.HasIndex(x => new { x.Barcode, x.Country });
        // 1 món / provider / id ngoài
        builder.HasIndex(x => new { x.ProviderCode, x.ExternalFoodId }).IsUnique()
            .HasFilter("[ProviderCode] IS NOT NULL AND [ExternalFoodId] IS NOT NULL");

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
        builder.Property(x => x.IsRepresentative).HasDefaultValue(false);
        builder.Property(x => x.IconKey).HasMaxLength(64).IsRequired(false);

        // Đúng 1 dòng đại diện / canonical
        builder.HasIndex(x => x.CanonicalFoodId).IsUnique().HasFilter("[IsRepresentative] = 1 AND [CanonicalFoodId] IS NOT NULL").HasDatabaseName("UX_FoodItems_Representative");
        builder.HasOne<CanonicalFood>().WithMany().HasForeignKey(x => x.CanonicalFoodId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // Macros per 100g — required
        builder.Property(x => x.CaloriesPer100g).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.ProteinPer100g).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.CarbsPer100g).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.FatPer100g).HasPrecision(8, 2).IsRequired();

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
        builder.Property(x => x.MillilitersEquivalent).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.UnitType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.SizeValue).HasPrecision(8, 3);

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