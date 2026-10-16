namespace Infrastructure;

public class WeightEntryConfiguration : SoftDeleteEntityConfiguration<WeightEntry, Guid>
{
    public override void Configure(EntityTypeBuilder<WeightEntry> builder)
    {
        base.Configure(builder);

        builder.ToTable("WeightEntries");
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).HasDefaultValue(WeightSource.Manual).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.TrendAlgorithmVersion).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.TrendConfidence).HasPrecision(5, 4).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        // Dedupe import từ Health: cùng nguồn + ExternalId chỉ 1 lần
        builder.HasIndex(x => new { x.UserId, x.Source, x.ExternalId }).IsUnique().HasFilter("[ExternalId] IS NOT NULL");

        // --- Properties ---
        builder.Property(x => x.WeightKg).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.TrendWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);

        // --- Indexes ---
        // 1 entry per user per day
        builder.HasIndex(x => new { x.UserId, x.LogDate }).IsUnique();
    }
}
