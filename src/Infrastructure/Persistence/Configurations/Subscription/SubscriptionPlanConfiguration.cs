namespace Infrastructure;

public class SubscriptionPlanConfiguration : BaseEntityConfiguration<SubscriptionPlan, Guid>
{
    public override void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        base.Configure(builder);

        builder.ToTable("SubscriptionPlans");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.PriceMonthly).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PriceYearly).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.Property(x => x.MaxCustomFoods).HasDefaultValue(-1);
        builder.Property(x => x.MaxCustomWorkouts).HasDefaultValue(-1);
        builder.Property(x => x.HasBarcodeScanning).HasDefaultValue(false);
        builder.Property(x => x.HasAdvancedAnalytics).HasDefaultValue(false);
        builder.Property(x => x.HasNutritionistAccess).HasDefaultValue(false);
        builder.Property(x => x.HasAiCoach).HasDefaultValue(false);

        builder.HasIndex(x => x.Name).IsUnique();

        // --- Relationships ---
        builder.HasMany(x => x.UserSubscriptions)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
