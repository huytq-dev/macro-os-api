namespace Infrastructure;

public class PeriodLogConfiguration : BaseEntityConfiguration<PeriodLog, Guid>
{
    public override void Configure(EntityTypeBuilder<PeriodLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("PeriodLogs");

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired(false);
        builder.Property(x => x.CycleLengthDays).IsRequired(false);
        builder.Property(x => x.PeriodLengthDays).IsRequired(false);
        builder.Property(x => x.Symptoms).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.StartDate });
    }
}
