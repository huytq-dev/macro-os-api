namespace Infrastructure;

public class GoalConfiguration : BaseEntityConfiguration<Goal, Guid>
{
    public override void Configure(EntityTypeBuilder<Goal> builder)
    {
        base.Configure(builder);

        builder.ToTable("Goals");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.TargetWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RatePerWeekKg).HasPrecision(4, 2).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProgressPhotoConfiguration : SoftDeleteEntityConfiguration<ProgressPhoto, Guid>
{
    public override void Configure(EntityTypeBuilder<ProgressPhoto> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProgressPhotos");

        builder.Property(x => x.Angle).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.CapturedAt });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

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
