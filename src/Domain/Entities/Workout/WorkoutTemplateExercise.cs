// Domain/Entities/Workout/WorkoutTemplateExercise.cs
using Domain;

public class WorkoutTemplateExercise : EntityBase<Guid>
{
    public Guid WorkoutTemplateId { get; set; }
    public Guid ExerciseId { get; set; }
    public int SortOrder { get; set; }
    public int DefaultSets { get; set; }
    public int? DefaultReps { get; set; }
    public decimal? DefaultWeight { get; set; }
    public int? DefaultDurationSeconds { get; set; }   // For cardio/timed
    public decimal? DefaultDistance { get; set; }       // km
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }

    public WorkoutTemplate Template { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}