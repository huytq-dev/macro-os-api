namespace Infrastructure;

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
