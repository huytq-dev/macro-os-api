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

public class FoodLogEntryNutrientConfiguration : BaseEntityConfiguration<FoodLogEntryNutrient, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogEntryNutrient> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogEntryNutrients", t => t.HasCheckConstraint(
            "CK_FoodLogEntryNutrients_AmountNonNegative", "[Amount] IS NULL OR [Amount] >= 0"));

        builder.Property(x => x.Amount).HasPrecision(12, 4).IsRequired(false);
        builder.Property(x => x.AmountStatus).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.FoodLogEntryId, x.NutrientDefinitionId }).IsUnique();

        builder.HasOne(x => x.FoodLogEntry).WithMany().HasForeignKey(x => x.FoodLogEntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Nutrient).WithMany().HasForeignKey(x => x.NutrientDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FoodScanAnalysisConfiguration : BaseEntityConfiguration<FoodScanAnalysis, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodScanAnalysis> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodScanAnalyses");

        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageRef).HasMaxLength(1024).IsRequired(false);
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.ModelVersion).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.ResultJson).IsRequired(false);
        builder.Property(x => x.Confidence).HasPrecision(5, 4).IsRequired(false);
        builder.Property(x => x.ErrorCode).HasMaxLength(50).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
