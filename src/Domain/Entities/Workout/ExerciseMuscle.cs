// Domain/Entities/Workout/ExerciseMuscle.cs
using Domain;

public class ExerciseMuscle : EntityBase<Guid>
{
    public Guid ExerciseId { get; set; }
    public MuscleGroup MuscleGroup { get; set; }
    public MuscleRole Role { get; set; }    // Primary, Secondary, Stabilizer
    public Exercise Exercise { get; set; } = null!;
}
