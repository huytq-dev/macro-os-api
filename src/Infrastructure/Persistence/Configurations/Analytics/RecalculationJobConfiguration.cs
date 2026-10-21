namespace Infrastructure;

public class RecalculationJobConfiguration : BaseEntityConfiguration<RecalculationJob, Guid>
{
    public override void Configure(EntityTypeBuilder<RecalculationJob> builder)
    {
        base.Configure(builder);

        builder.ToTable("RecalculationJobs");

        builder.Property(x => x.Cause).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000).IsRequired(false);

        builder.HasIndex(x => new { x.State, x.CreatedAt });
        // Idempotent: không xếp 2 job Pending trùng khoảng ngày cho cùng user
        builder.HasIndex(x => new { x.UserId, x.FromDate, x.ToDate }).IsUnique().HasFilter("[State] = 'Pending'");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
