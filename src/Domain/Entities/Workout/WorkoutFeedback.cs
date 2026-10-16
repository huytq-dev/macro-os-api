namespace Domain;

// Phản hồi tuỳ chọn (RP-style); không bắt buộc để finish session
public class WorkoutFeedback : EntityBase<Guid>
{
    public Guid WorkoutSessionId { get; set; }
    public Guid? WorkoutSessionExerciseId { get; set; }
    public int? PerceivedEffort { get; set; }       // 1-10
    public int? Pump { get; set; }
    public int? Soreness { get; set; }
    public int? Recovery { get; set; }
    public string? Comment { get; set; }

    public WorkoutSession WorkoutSession { get; set; } = null!;
    public WorkoutSessionExercise? WorkoutSessionExercise { get; set; }
}
