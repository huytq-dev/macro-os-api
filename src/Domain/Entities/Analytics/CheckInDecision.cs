namespace Domain;

// Audit quyết định của user; tối đa 1 quyết định / recommendation
public class CheckInDecision : EntityBase<Guid>
{
    public Guid CoachingRecommendationId { get; set; }
    public Guid UserId { get; set; }
    public CheckInDecisionType Decision { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
    public Guid? CreatedMacroProgramWeekId { get; set; }    // chỉ có khi Accept

    public CoachingRecommendation Recommendation { get; set; } = null!;
    public MacroProgramWeek? CreatedMacroProgramWeek { get; set; }
}
