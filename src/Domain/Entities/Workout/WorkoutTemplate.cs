// Domain/Entities/Workout/WorkoutTemplate.cs
// Workout plan/program được tạo sẵn
using Domain;

public class WorkoutTemplate : EntityBase<Guid>, ISoftDeletable
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool IsPublic { get; set; }
    public bool IsSystemTemplate { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public WorkoutGoal WorkoutGoal { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<WorkoutTemplateExercise> Exercises { get; set; } = [];
    public ICollection<WorkoutSession> Sessions { get; set; } = [];
}
