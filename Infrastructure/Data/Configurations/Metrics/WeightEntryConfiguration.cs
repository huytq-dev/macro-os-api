namespace Infrastructure;

public class WeightEntryConfiguration : SoftDeleteEntityConfiguration<WeightEntry, Guid>
{
    public override void Configure(EntityTypeBuilder<WeightEntry> builder)
    {
        base.Configure(builder);

        builder.ToTable("WeightEntries");

        // --- Properties ---
        builder.Property(x => x.WeightKg).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.TrendWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);

        // --- Indexes ---
        // 1 entry per user per day
        builder.HasIndex(x => new { x.UserId, x.LogDate }).IsUnique();
    }
}
