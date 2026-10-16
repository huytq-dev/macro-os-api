namespace Domain;

public class WorkoutDayTemplate : EntityBase<Guid>
{
    public Guid WorkoutProgramRevisionId { get; set; }
    public int DayIndex { get; set; }               // 1..DaysPerCycle
    public string Name { get; set; } = null!;
    public bool IsRestDay { get; set; }
    public Guid? GymProfileId { get; set; }         // override gym của revision cho ngày này
    public int SortOrder { get; set; }

    public WorkoutProgramRevision Revision { get; set; } = null!;
    public GymProfile? GymProfile { get; set; }
    public ICollection<WorkoutExercisePrescription> Prescriptions { get; set; } = [];
}
