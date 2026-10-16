namespace Infrastructure;

public class UserSubscriptionConfiguration : BaseEntityConfiguration<UserSubscription, Guid>
{
    public override void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserSubscriptions");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BillingCycle)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired(false);

        builder.Property(x => x.PaymentProvider)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.ExternalSubscriptionId)
            .HasMaxLength(256)
            .IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.ExternalSubscriptionId)
            .HasFilter("[ExternalSubscriptionId] IS NOT NULL");
    }
}
