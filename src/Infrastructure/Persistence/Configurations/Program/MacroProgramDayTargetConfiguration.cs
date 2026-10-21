namespace Infrastructure;

public class MacroProgramDayTargetConfiguration : BaseEntityConfiguration<MacroProgramDayTarget, Guid>
{
    public override void Configure(EntityTypeBuilder<MacroProgramDayTarget> builder)
    {
        base.Configure(builder);

        builder.ToTable("MacroProgramDayTargets");
        builder.Property(x => x.DayType).HasConversion<string>().HasMaxLength(30).HasDefaultValue(DayType.Standard).IsRequired();
        builder.Property(x => x.AdditionalTargetsJson).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IsFastingDay).HasDefaultValue(false);
        builder.Property(x => x.IsLocked).HasDefaultValue(false);

        builder.Property(x => x.CaloriesTarget).IsRequired(false);
        builder.Property(x => x.ProteinTarget).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.CarbsTarget).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.FatTarget).HasPrecision(6, 2).IsRequired(false);

        // --- Indexes ---
        // Unique: 1 day config per program per day of week
        builder.HasIndex(x => new { x.MacroProgramId, x.DayOfWeek }).IsUnique();
    }
}
