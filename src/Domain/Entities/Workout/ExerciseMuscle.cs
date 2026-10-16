// Domain/Entities/Workout/ExerciseMuscle.cs
using Domain;

public class ExerciseMuscle : EntityBase<Guid>
{
    public Guid ExerciseId { get; set; }
    public MuscleGroup MuscleGroup { get; set; }
    public MuscleRole Role { get; set; }    // Primary, Secondary, Stabilizer
    public decimal? ContributionWeight { get; set; }        // 0..1, dùng đếm hard set cho cơ phụ
    public Exercise Exercise { get; set; } = null!;
}
