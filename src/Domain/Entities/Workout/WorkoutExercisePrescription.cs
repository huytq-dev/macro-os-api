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
    public string? Tempo { get; set; }                      // vd 3-1-1-0
    public LoadType LoadType { get; set; } = LoadType.Weighted;
    public decimal? MinLoadKg { get; set; }                 // giới hạn riêng của bài
    public decimal? MaxLoadKg { get; set; }

    public WorkoutDayTemplate DayTemplate { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public ICollection<ExerciseTargetByCycle> Targets { get; set; } = [];
}
