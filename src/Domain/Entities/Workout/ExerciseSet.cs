// Domain/Entities/Workout/ExerciseSet.cs
using Domain;

public class ExerciseSet : EntityBase<Guid>
{
    public Guid WorkoutSessionExerciseId { get; set; }
    public int SetNumber { get; set; }
    public SetType SetType { get; set; }            // Normal, Warmup, Drop, Failure
    public int? Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public int? DurationSeconds { get; set; }       // Plank, cardio
    public decimal? DistanceKm { get; set; }
    public int? Rpm { get; set; }                   // Cycling
    public bool IsCompleted { get; set; }
    public decimal? Rir { get; set; }               // Reps in Reserve
    public int? Rpe { get; set; }                   // Rate of Perceived Exertion

    public WorkoutSessionExercise SessionExercise { get; set; } = null!;
}
