// Domain/Entities/Subscription/UserSubscription.cs
using Domain;

public class UserSubscription : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? PaymentProvider { get; set; }    // "Stripe", "VNPay"
    public string? ExternalSubscriptionId { get; set; }

    public User User { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
