namespace Infrastructure;

public class MacroProgramWeekConfiguration : BaseEntityConfiguration<MacroProgramWeek, Guid>
{
    public override void Configure(EntityTypeBuilder<MacroProgramWeek> builder)
    {
        base.Configure(builder);

        builder.ToTable("MacroProgramWeeks");
        builder.Property(x => x.RevisionNo);
        builder.HasIndex(x => new { x.MacroProgramId, x.RevisionNo }).IsUnique();
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.PreferencesJson).HasMaxLength(2000).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.CaloriesTarget).IsRequired();
        builder.Property(x => x.ProteinTarget).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.CarbsTarget).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.FatTarget).HasPrecision(6, 2).IsRequired();

        builder.Property(x => x.AdjustmentReason)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PreviousCaloriesTarget).IsRequired(false);

        // --- Indexes ---
        // Unique: 1 target row per program per week start date
        builder.HasIndex(x => new { x.MacroProgramId, x.EffectiveFrom }).IsUnique();
    }
}
