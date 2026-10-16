namespace Domain;

// Gợi ý liên module (vd dịch calorie sang ngày tập); không tự áp dụng, cần user xác nhận; flag CrossDomainCoaching mặc định OFF
public class CrossDomainRecommendation : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public CrossDomainType Type { get; set; }
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Proposed;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public User User { get; set; } = null!;
}
