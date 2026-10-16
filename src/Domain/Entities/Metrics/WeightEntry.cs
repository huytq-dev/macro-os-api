namespace Domain;

public class WeightEntry : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public decimal WeightKg { get; set; }

    // Exponential moving average (20-day window) — computed by algorithm
    public decimal? TrendWeightKg { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset? MeasuredAt { get; set; }         // thời điểm cân thực (UTC); LogDate là ngày local
    public WeightSource Source { get; set; } = WeightSource.Manual;
    public string? ExternalId { get; set; }                 // id từ Health Connect/Apple Health để dedupe
    public string? TrendAlgorithmVersion { get; set; }
    public decimal? TrendConfidence { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
}
