namespace Infrastructure;

public class StepEntryConfiguration : BaseEntityConfiguration<StepEntry, Guid>
{
    public override void Configure(EntityTypeBuilder<StepEntry> builder)
    {
        base.Configure(builder);

        builder.ToTable("StepEntries", t => t.HasCheckConstraint("CK_StepEntries_Steps", "[Steps] >= 0"));

        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).IsRequired();

        // 1 dòng/user/ngày, như WeightEntries
        builder.HasIndex(x => new { x.UserId, x.LogDate }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
