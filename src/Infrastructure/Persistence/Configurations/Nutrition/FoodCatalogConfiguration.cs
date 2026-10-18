namespace Infrastructure;

public class NutrientDefinitionConfiguration : BaseEntityConfiguration<NutrientDefinition, Guid>
{
    public override void Configure(EntityTypeBuilder<NutrientDefinition> builder)
    {
        base.Configure(builder);

        builder.ToTable("NutrientDefinitions");

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameVi).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.Unit).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Group).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceMin).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.ReferenceTarget).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.ReferenceMax).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.UsdaNumber).HasMaxLength(10).IsRequired(false);
        builder.Property(x => x.IsCore).IsRequired();

        // Mỗi số chất USDA ứng với đúng một chất của MacroOS (importer tra theo số này)
        builder.HasIndex(x => x.UsdaNumber).IsUnique().HasFilter("[UsdaNumber] IS NOT NULL");

        builder.HasIndex(x => x.Code).IsUnique();
    }
}

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

public class FoodAliasConfiguration : BaseEntityConfiguration<FoodAlias, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodAlias> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodAliases");

        builder.Property(x => x.Locale).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AliasNormalized).HasMaxLength(256).IsRequired();

        builder.HasIndex(x => new { x.Locale, x.AliasNormalized });
        builder.HasOne<CanonicalFood>().WithMany().HasForeignKey(x => x.CanonicalFoodId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FoodImageConfiguration : BaseEntityConfiguration<FoodImage, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodImage> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodImages");

        builder.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.LicenseMetadata).HasMaxLength(500).IsRequired(false);

        // Tối đa 1 ảnh primary / món
        builder.HasIndex(x => x.FoodItemId).IsUnique().HasFilter("[IsPrimary] = 1");

        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserFoodIconConfiguration : BaseEntityConfiguration<UserFoodIcon, Guid>
{
    public override void Configure(EntityTypeBuilder<UserFoodIcon> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserFoodIcons");

        builder.Property(x => x.IconKey).HasMaxLength(64).IsRequired();
        // Mỗi user chỉ có một icon riêng cho một món
        builder.HasIndex(x => new { x.UserId, x.FoodItemId }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FoodFavoriteConfiguration : BaseEntityConfiguration<FoodFavorite, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodFavorite> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodFavorites");

        builder.Property(x => x.Quantity).HasPrecision(10, 2);
        // Một preset = (món, khẩu phần, số lượng); cùng món được nhiều preset khác nhau.
        builder.HasIndex(x => new { x.UserId, x.FoodItemId, x.FoodServingId, x.Quantity }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.SortOrder });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict);
        // UpdateCustomFood thay toàn bộ serving: preset dùng serving đã mất bị xoá theo (không giữ preset trỏ vào khẩu phần không còn).
        builder.HasOne(x => x.FoodServing).WithMany().HasForeignKey(x => x.FoodServingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FoodImportRunConfiguration : BaseEntityConfiguration<FoodImportRun, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodImportRun> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodImportRuns");

        builder.Property(x => x.Provider).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Query).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000).IsRequired(false);

        builder.HasIndex(x => new { x.Provider, x.StartedAt });
    }
}

public class CanonicalFoodConfiguration : BaseEntityConfiguration<CanonicalFood, Guid>
{
    public override void Configure(EntityTypeBuilder<CanonicalFood> builder)
    {
        base.Configure(builder);

        builder.ToTable("CanonicalFoods");

        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NameNormalized).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NameVi).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DefaultServingGrams).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.DensityGPerMl).HasPrecision(8, 4).IsRequired(false);

        builder.HasIndex(x => x.NameNormalized).IsUnique();
    }
}

public class FoodSourceRecordConfiguration : BaseEntityConfiguration<FoodSourceRecord, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodSourceRecord> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodSourceRecords");

        builder.Property(x => x.Provider).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.MatchConfidence).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.MatchMethod).HasMaxLength(30).IsRequired();

        // Một bản ghi nguồn thuộc đúng một canonical
        builder.HasIndex(x => x.FoodItemId).IsUnique();
        builder.HasIndex(x => new { x.CanonicalFoodId, x.Priority });

        builder.HasOne(x => x.Canonical).WithMany(x => x.Sources).HasForeignKey(x => x.CanonicalFoodId).OnDelete(DeleteBehavior.Cascade);
        // Restrict: FoodItem đã có đường Cascade khác tới FoodNutrients/Servings, tránh multiple cascade path
        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

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
