// Domain/Entities/Notifications/Notification.cs
using Domain;

public class Notification : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? MetaData { get; set; }       // JSON extra data

    public User User { get; set; } = null!;
}
