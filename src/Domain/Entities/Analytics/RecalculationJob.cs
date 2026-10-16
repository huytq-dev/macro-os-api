namespace Domain;

// Tính lại trend/TDEE trong [FromDate, ToDate] khi dữ liệu quá khứ bị sửa; chạy idempotent, retry có giới hạn
public class RecalculationJob : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public RecalculationCause Cause { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public JobState State { get; set; } = JobState.Pending;
    public string AlgorithmVersion { get; set; } = "1";
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public User User { get; set; } = null!;
}
