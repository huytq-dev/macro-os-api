namespace Infrastructure;

public class ReminderSettingConfiguration : BaseEntityConfiguration<ReminderSetting, Guid>
{
    public override void Configure(EntityTypeBuilder<ReminderSetting> builder)
    {
        base.Configure(builder);

        builder.ToTable("ReminderSettings");

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsEnabled).HasDefaultValue(true);
        builder.Property(x => x.ReminderTime).IsRequired();

        // Store flags enum as int
        builder.Property(x => x.DaysOfWeek)
            .HasConversion<int>()
            .IsRequired();

        // Unique: 1 reminder per type per user
        builder.HasIndex(x => new { x.UserId, x.Type })
            .IsUnique();
    }
}