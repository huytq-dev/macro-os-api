namespace Infrastructure;

public class FoodLogEntryNutrientConfiguration : BaseEntityConfiguration<FoodLogEntryNutrient, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogEntryNutrient> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogEntryNutrients", t => t.HasCheckConstraint(
            "CK_FoodLogEntryNutrients_AmountNonNegative", "[Amount] IS NULL OR [Amount] >= 0"));

        builder.Property(x => x.Amount).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.AmountStatus).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.FoodLogEntryId, x.NutrientDefinitionId }).IsUnique();

        builder.HasOne(x => x.FoodLogEntry).WithMany().HasForeignKey(x => x.FoodLogEntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Nutrient).WithMany().HasForeignKey(x => x.NutrientDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
