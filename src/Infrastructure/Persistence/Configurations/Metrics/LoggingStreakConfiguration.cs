namespace Infrastructure;

public class LoggingStreakConfiguration : BaseEntityConfiguration<LoggingStreak, Guid>
{
    public override void Configure(EntityTypeBuilder<LoggingStreak> builder)
    {
        base.Configure(builder);

        builder.ToTable("LoggingStreaks");

        builder.Property(x => x.StreakType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CurrentStreak).IsRequired();
        builder.Property(x => x.LongestStreak).IsRequired();
        builder.Property(x => x.LastLogDate).IsRequired();

        // --- Indexes ---
        // Unique: 1 streak record per type per user
        builder.HasIndex(x => new { x.UserId, x.StreakType }).IsUnique();
    }
}
