namespace Domain;

public class WeightEntry : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public decimal WeightKg { get; set; }

    // Exponential moving average (20-day window) — computed by algorithm
    public decimal? TrendWeightKg { get; set; }

    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
}
