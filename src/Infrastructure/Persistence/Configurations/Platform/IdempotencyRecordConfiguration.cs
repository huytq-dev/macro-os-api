namespace Infrastructure;

public class IdempotencyRecordConfiguration : BaseEntityConfiguration<IdempotencyRecord, Guid>
{
    public override void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        base.Configure(builder);

        builder.ToTable("IdempotencyRecords");

        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResponseBody).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.Key, x.Endpoint }).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
