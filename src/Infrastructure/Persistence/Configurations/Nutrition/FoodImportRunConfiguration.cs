namespace Infrastructure;

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
