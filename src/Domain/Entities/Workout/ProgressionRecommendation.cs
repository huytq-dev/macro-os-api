namespace Domain;

// Gợi ý tải/rep cho buổi sau, có lý do + version; không bao giờ ghi đè set đã log
public class ProgressionRecommendation : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid? WorkoutExercisePrescriptionId { get; set; }
    public Guid ReferenceSessionId { get; set; }
    public decimal? SuggestedWeightKg { get; set; }
    public int? SuggestedReps { get; set; }
    public ProgressionAction Action { get; set; }
    public string ReasonCodesJson { get; set; } = "[]";
    public string AlgorithmVersion { get; set; } = null!;
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Proposed;
    public decimal? Confidence { get; set; }

    public User User { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public WorkoutSession ReferenceSession { get; set; } = null!;
}
