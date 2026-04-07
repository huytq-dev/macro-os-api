using Domain;

public class SubscriptionPlan : EntityBase<Guid>
{
    public string Name { get; set; } = null!;        // "Free", "Pro", "Premium"
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public bool IsActive { get; set; } = true;

    // Feature flags
    public int MaxCustomFoods { get; set; }          // -1 = unlimited
    public int MaxCustomWorkouts { get; set; }
    public bool HasBarcodeScanning { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
    public bool HasNutritionistAccess { get; set; }
    public bool HasAiCoach { get; set; }

    public ICollection<UserSubscription> UserSubscriptions { get; set; } = [];
}