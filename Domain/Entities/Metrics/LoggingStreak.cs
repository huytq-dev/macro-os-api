namespace Domain;

// Streak (chuỗi ngày log liên tiếp) cho từng loại activity.
// 1 row per StreakType per User (enforced bằng unique index).
// Được cập nhật mỗi khi user log entry mới.
public class LoggingStreak : EntityBase<Guid>
{
    public Guid UserId { get; set; }

    public StreakType StreakType { get; set; }

    // Số ngày log liên tiếp tính đến hôm nay
    public int CurrentStreak { get; set; }

    // Kỷ lục
    public int LongestStreak { get; set; }

    // Ngày log gần nhất — dùng để tính xem streak có bị break không
    public DateOnly LastLogDate { get; set; }

    public User User { get; set; } = null!;
}
