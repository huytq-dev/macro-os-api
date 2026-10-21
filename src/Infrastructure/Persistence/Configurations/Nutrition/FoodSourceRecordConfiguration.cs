namespace Infrastructure;

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
