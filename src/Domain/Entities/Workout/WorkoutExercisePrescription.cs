namespace Domain;

public class WorkoutExercisePrescription : EntityBase<Guid>
{
    public Guid WorkoutDayTemplateId { get; set; }
    public Guid ExerciseId { get; set; }
    public int SortOrder { get; set; }
    public int? SupersetGroup { get; set; }         // cùng số = cùng superset
    public string? Notes { get; set; }
    public int? RestSeconds { get; set; }
    public ProgressionPolicy ProgressionPolicy { get; set; } = ProgressionPolicy.DoubleProgression;

    public WorkoutDayTemplate DayTemplate { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public ICollection<ExerciseTargetByCycle> Targets { get; set; } = [];
}
