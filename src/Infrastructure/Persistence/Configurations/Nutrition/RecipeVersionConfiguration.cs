namespace Infrastructure;

public class RecipeVersionConfiguration : BaseEntityConfiguration<RecipeVersion, Guid>
{
    public override void Configure(EntityTypeBuilder<RecipeVersion> builder)
    {
        base.Configure(builder);

        builder.ToTable("RecipeVersions");

        builder.Property(x => x.Servings).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.CookedYieldGrams).HasPrecision(10, 2).IsRequired(false);
        builder.Property(x => x.RawTotalGrams).HasPrecision(10, 2).IsRequired(false);
        builder.Property(x => x.Instructions).HasMaxLength(4000).IsRequired(false);
        builder.Property(x => x.IngredientsSnapshotJson).IsRequired();
        builder.Property(x => x.TotalCalories).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.TotalProtein).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.TotalCarbs).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.TotalFat).HasPrecision(10, 2).IsRequired();

        builder.HasIndex(x => new { x.RecipeId, x.VersionNo }).IsUnique();

        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}
