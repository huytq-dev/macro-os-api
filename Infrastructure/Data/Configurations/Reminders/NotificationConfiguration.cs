namespace Infrastructure;

public class NotificationConfiguration : BaseEntityConfiguration<Notification, Guid>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);

        builder.ToTable("Notifications");

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsRead).HasDefaultValue(false);
        builder.Property(x => x.ReadAt).IsRequired(false);
        builder.Property(x => x.ActionUrl).HasMaxLength(1024).IsRequired(false);
        builder.Property(x => x.MetaData).HasMaxLength(2000).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.IsRead });
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
