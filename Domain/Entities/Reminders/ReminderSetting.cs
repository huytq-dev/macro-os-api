// Domain/Entities/Notifications/ReminderSetting.cs
using Domain;

public class ReminderSetting : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public ReminderType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public TimeOnly ReminderTime { get; set; }
    public DaysOfWeek DaysOfWeek { get; set; }  // Flags enum

    public User User { get; set; } = null!;
}
