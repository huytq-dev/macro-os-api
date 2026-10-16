namespace Domain;

// Target của 1 bài trong 1 cycle; thiếu row = kế thừa cycle trước (carry-forward)
public class ExerciseTargetByCycle : EntityBase<Guid>
{
    public Guid WorkoutExercisePrescriptionId { get; set; }
    public int CycleIndex { get; set; }
    public int TargetSets { get; set; }
    public int RepMin { get; set; }
    public int RepMax { get; set; }
    public decimal? Rir { get; set; }
    public decimal? LoadTargetKg { get; set; }
    public string? SetTypeTemplateJson { get; set; }    // loại set theo thứ tự: warmup, drop, failure...

    public WorkoutExercisePrescription Prescription { get; set; } = null!;
}
