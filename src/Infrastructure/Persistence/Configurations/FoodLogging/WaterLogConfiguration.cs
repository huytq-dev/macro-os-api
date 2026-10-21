namespace Infrastructure;

public class WaterLogConfiguration : BaseEntityConfiguration<WaterLog, Guid>
{
    public override void Configure(EntityTypeBuilder<WaterLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("WaterLogs");

        builder.Property(x => x.AmountMl).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.LoggedAt).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(50).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.LoggedAt });
    }
}
