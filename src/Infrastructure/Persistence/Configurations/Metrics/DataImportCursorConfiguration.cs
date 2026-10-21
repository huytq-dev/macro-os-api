namespace Infrastructure;

public class DataImportCursorConfiguration : BaseEntityConfiguration<DataImportCursor, Guid>
{
    public override void Configure(EntityTypeBuilder<DataImportCursor> builder)
    {
        base.Configure(builder);

        builder.ToTable("DataImportCursors");

        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.SyncStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResourceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Cursor).HasMaxLength(1000).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.Provider, x.ResourceType }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
