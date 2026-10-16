namespace Domain;

public class WorkoutProgramRevision : EntityBase<Guid>
{
    public Guid WorkoutProgramId { get; set; }
    public int RevisionNo { get; set; }
    public int CycleCount { get; set; } = 1;
    public int DaysPerCycle { get; set; } = 7;      // cycle không nhất thiết theo tuần (1–14 ngày)
    public bool PeriodizationEnabled { get; set; }
    public Guid? GymProfileId { get; set; }
    public string? GeneratorVersion { get; set; }   // null nếu tạo tay
    public string? ConfigJson { get; set; }         // input Smart Generation để tái hiện
    public DateTimeOffset? PublishedAt { get; set; }
    public DateOnly? EffectiveFrom { get; set; }

    public WorkoutProgram WorkoutProgram { get; set; } = null!;
    public GymProfile? GymProfile { get; set; }
    public ICollection<WorkoutDayTemplate> Days { get; set; } = [];
}
