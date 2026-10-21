namespace Infrastructure;

public class ProgressPhotoConfiguration : SoftDeleteEntityConfiguration<ProgressPhoto, Guid>
{
    public override void Configure(EntityTypeBuilder<ProgressPhoto> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProgressPhotos");

        builder.Property(x => x.Angle).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.CapturedAt });
        // Mỗi (ngày, góc) một ảnh còn hiệu lực (ảnh đã xoá mềm không chặn chụp lại)
        builder.HasIndex(x => new { x.UserId, x.PhotoDate, x.Angle }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
