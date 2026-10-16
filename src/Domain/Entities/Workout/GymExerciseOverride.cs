namespace Domain;

public class GymExerciseOverride : EntityBase<Guid>
{
    public Guid GymProfileId { get; set; }
    public Guid ExerciseId { get; set; }
    public GymExerciseRule Rule { get; set; }
    public string? Reason { get; set; }

    public GymProfile GymProfile { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}
