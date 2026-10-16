namespace Domain;

// Projection, rebuild được từ ExerciseSets (loại warm-up, set chưa hoàn thành)
public class PersonalRecord : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public PersonalRecordMetric Metric { get; set; }
    public decimal Value { get; set; }
    public Guid? ExerciseSetId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public User User { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}
