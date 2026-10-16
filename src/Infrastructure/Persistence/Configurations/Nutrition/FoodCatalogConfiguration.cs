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
        builder.Property(x => x.ReferenceMax).HasPrecision(12, 4).IsRequired(false);

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

public class FoodFavoriteConfiguration : BaseEntityConfiguration<FoodFavorite, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodFavorite> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodFavorites");

        builder.HasIndex(x => new { x.UserId, x.FoodItemId }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
