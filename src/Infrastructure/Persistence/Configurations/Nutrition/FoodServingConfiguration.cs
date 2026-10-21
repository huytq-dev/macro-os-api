namespace Infrastructure;

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
