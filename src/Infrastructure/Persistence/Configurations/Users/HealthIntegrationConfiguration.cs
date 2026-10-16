namespace Infrastructure;

public class HealthIntegrationConfiguration : BaseEntityConfiguration<HealthIntegration, Guid>
{
    public override void Configure(EntityTypeBuilder<HealthIntegration> builder)
    {
        base.Configure(builder);

        builder.ToTable("HealthIntegrations");

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsEnabled).HasDefaultValue(false);
        builder.Property(x => x.LastSyncAt).IsRequired(false);
        builder.Property(x => x.SyncSteps).HasDefaultValue(true);
        builder.Property(x => x.SyncWeight).HasDefaultValue(true);
        builder.Property(x => x.SyncWorkouts).HasDefaultValue(true);
        builder.Property(x => x.ExternalUserId).HasMaxLength(256).IsRequired(false);

        // --- Indexes ---
        // Unique: 1 integration per provider per user
        builder.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
    }
}
