namespace Domain;

// Ảnh tiến độ private: chỉ lưu StorageKey, truy cập qua signed URL
public class ProgressPhoto : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public PhotoAngle Angle { get; set; }
    public string StorageKey { get; set; } = null!;
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset? ConsentGivenAt { get; set; }
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
}
