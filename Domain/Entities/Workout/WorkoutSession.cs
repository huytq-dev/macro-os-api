// Domain/Entities/Workout/WorkoutSession.cs
// 1 buổi tập thực tế của user
using Domain;

public class WorkoutSession : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public Guid? WorkoutTemplateId { get; set; }    // Null = custom session
    public string Name { get; set; } = null!;
    public DateOnly SessionDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public WorkoutSessionStatus Status { get; set; } = WorkoutSessionStatus.InProgress;
    public decimal? CaloriesBurned { get; set; }
    public int? PerceivedExertion { get; set; }     // RPE 1-10
    public string? Notes { get; set; }
    public string? MoodBefore { get; set; }
    public string? MoodAfter { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public WorkoutTemplate? Template { get; set; }
    public ICollection<WorkoutSessionExercise> Exercises { get; set; } = [];
}
