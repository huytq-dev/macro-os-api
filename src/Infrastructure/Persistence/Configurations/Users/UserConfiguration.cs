namespace Infrastructure;

public class UserConfiguration : SoftDeleteEntityConfiguration<User, Guid>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("Users");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).HasDefaultValue(UserStatus.Active).IsRequired();

        // --- Properties ---
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(1024)
            .IsRequired(false);

        builder.Property(x => x.RefreshToken)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EmailVerified)
            .HasDefaultValue(false);

        builder.Property(x => x.RefreshTokenExpiry)
            .IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.PhoneNumber)
            .IsUnique();

        // --- Relationships ---
        builder.HasOne(x => x.Profile)
            .WithOne(x => x.User)
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MacroPrograms)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.WeightEntries)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FoodLogs)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.WorkoutSessions)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BodyMetrics)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict để tránh multiple cascade paths: User → FoodLog → WaterLog cũng là Cascade
        builder.HasMany(x => x.WaterLogs)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Notifications)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReminderSettings)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Subscriptions)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // NoAction: các bảng này có thêm FK tới MacroProgramWeek (qua User → MacroProgram → MacroProgramWeek)
        // → SQL Server không cho phép multiple cascade paths đến cùng 1 bảng
        builder.HasMany(x => x.DailyProgressSnapshots)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.ExpenditureSnapshots)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.WeeklyCheckIns)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}