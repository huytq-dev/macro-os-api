namespace Domain;

// Projection volume theo nhóm cơ / tuần (PeriodStart = ngày đầu tuần theo UserProfile)
public class WorkoutMuscleAggregate : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public MuscleGroup MuscleGroup { get; set; }
    public decimal HardSets { get; set; }
    public decimal VolumeKg { get; set; }
    public int SessionCount { get; set; }

    public User User { get; set; } = null!;
}
