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
