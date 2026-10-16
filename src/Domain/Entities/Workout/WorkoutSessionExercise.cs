// Domain/Entities/Workout/WorkoutSessionExercise.cs
using Domain;

public class WorkoutSessionExercise : EntityBase<Guid>
{
    public Guid WorkoutSessionId { get; set; }
    public Guid ExerciseId { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public int? RestSeconds { get; set; }
    public Guid? OriginalPrescriptionId { get; set; }
    public int? SupersetGroup { get; set; }
    public Guid? ReplacedExerciseId { get; set; }           // bài gốc nếu đã đổi giữa buổi
    public string? ReplacementReason { get; set; }

    public WorkoutSession Session { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public ICollection<ExerciseSet> Sets { get; set; } = [];
}