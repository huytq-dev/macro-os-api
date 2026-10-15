namespace Infrastructure;

public class UserProfileConfiguration : BaseEntityConfiguration<UserProfile, Guid>
{
    public override void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserProfiles");

        // --- Properties ---
        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DateOfBirth)
            .IsRequired(false);

        builder.Property(x => x.Bio)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.TimeZone)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Locale)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.UnitPreference)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(UnitSystem.Metric);

        builder.Property(x => x.ActivityLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TrainingType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TrainingType.None);

        builder.Property(x => x.HeightCm)
            .HasPrecision(5, 2)
            .IsRequired(false);
    }
}