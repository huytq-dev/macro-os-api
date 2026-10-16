namespace Domain;

// Trạng thái kết nối với Apple Health / Google Fit.
// 1 row per Provider per User (enforced bằng unique index).
public class HealthIntegration : EntityBase<Guid>
{
    public Guid UserId { get; set; }

    public HealthProvider Provider { get; set; }

    public bool IsEnabled { get; set; }

    public DateTimeOffset? LastSyncAt { get; set; }

    // Granular sync toggles — user chọn sync loại data nào
    public bool SyncSteps { get; set; } = true;
    public bool SyncWeight { get; set; } = true;
    public bool SyncWorkouts { get; set; } = true;

    // ID phía provider (Apple Health không có, Google Fit có)
    public string? ExternalUserId { get; set; }

    public User User { get; set; } = null!;
}
