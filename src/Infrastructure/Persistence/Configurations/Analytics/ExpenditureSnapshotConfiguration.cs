namespace Infrastructure;

public class ExpenditureSnapshotConfiguration : BaseEntityConfiguration<ExpenditureSnapshot, Guid>
{
    public override void Configure(EntityTypeBuilder<ExpenditureSnapshot> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExpenditureSnapshots");
        builder.Property(x => x.QualityScore).HasPrecision(5, 4).IsRequired(false);
        builder.Property(x => x.Completeness).HasPrecision(5, 4).IsRequired(false);

        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).HasDefaultValue("1").IsRequired();
        builder.Property(x => x.CalculationRevision).IsRequired(false);
        builder.Property(x => x.ReasonCodesJson).HasMaxLength(1000).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.EstimatedTdeeKcal).HasPrecision(8, 2).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AvgCaloriesConsumed).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.WeightChangeRateKgPerWeek).HasPrecision(5, 3).IsRequired(false);
        builder.Property(x => x.WindowDays).IsRequired();

        // --- Indexes ---
        // 1 snapshot per user per day
        builder.HasIndex(x => new { x.UserId, x.SnapshotDate }).IsUnique();
    }
}
