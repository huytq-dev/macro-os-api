namespace Domain;

// Đề xuất của engine, bất biến sau khi tạo; chỉ Status đổi theo quyết định của user
public class CoachingRecommendation : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid MacroProgramId { get; set; }
    public Guid? OldMacroProgramWeekId { get; set; }        // revision đang áp dụng lúc sinh đề xuất
    public CoachingRecommendationType Type { get; set; }
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Proposed;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string ProposedTargetsJson { get; set; } = "{}";
    public string ReasonCodesJson { get; set; } = "[]";
    public decimal? Confidence { get; set; }
    public string AlgorithmVersion { get; set; } = null!;
    public DateTimeOffset GeneratedAt { get; set; }

    public User User { get; set; } = null!;
    public MacroProgram MacroProgram { get; set; } = null!;
}
