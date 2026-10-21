namespace Infrastructure;

public class EquipmentConfiguration : BaseEntityConfiguration<Equipment, Guid>
{
    public override void Configure(EntityTypeBuilder<Equipment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Equipment");

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DefaultIncrementKg).HasPrecision(5, 2).IsRequired(false);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
