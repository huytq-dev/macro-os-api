namespace Infrastructure;

public class FoodLogBatchConfiguration : BaseEntityConfiguration<FoodLogBatch, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogBatch> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogBatches");

        builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.MetadataJson).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Restrict (không SetNull): đã có đường Cascade User → FoodLogs → Entries, tránh multiple cascade path
        builder.HasMany<FoodLogEntry>().WithOne().HasForeignKey(x => x.FoodLogBatchId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
