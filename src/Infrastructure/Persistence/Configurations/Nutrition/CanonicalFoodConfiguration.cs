namespace Infrastructure;

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
