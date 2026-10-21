namespace Infrastructure;

public class ExportJobConfiguration : BaseEntityConfiguration<ExportJob, Guid>
{
    public override void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExportJobs");

        builder.Property(x => x.JobType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired(false);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.RequestedAt });

        // Không Cascade: job xoá dữ liệu phải sống sót tới khi hoàn tất
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
