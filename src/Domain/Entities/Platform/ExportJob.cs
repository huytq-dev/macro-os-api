namespace Domain;

// Yêu cầu export dữ liệu hoặc xoá toàn bộ dữ liệu cá nhân
public class ExportJob : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public ExportJobType JobType { get; set; }
    public string? Format { get; set; }             // csv, json
    public JobState Status { get; set; } = JobState.Pending;
    public string? StorageKey { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }

    public User User { get; set; } = null!;
}
