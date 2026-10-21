namespace Infrastructure;

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
