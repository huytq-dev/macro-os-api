namespace Domain;

// Con trỏ đồng bộ resumable từ Apple Health / Google Fit theo từng loại dữ liệu
public class DataImportCursor : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public HealthProvider Provider { get; set; }
    public string ResourceType { get; set; } = null!;   // steps, weight, workouts
    public string? Cursor { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Idle;

    public User User { get; set; } = null!;
}
