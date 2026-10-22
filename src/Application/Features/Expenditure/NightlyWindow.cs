namespace Application;

/// <summary>
/// Cửa sổ job đêm theo giờ LOCAL của user: job chạy mỗi giờ, mỗi user chỉ được xử lý ở lần chạy rơi vào giờ local 00 (đúng một lần/ngày local, kể cả múi giờ lệch :30/:45).
/// Hàm thuần, không trạng thái: nếu một lần chạy bị lỡ thì lần dựng khi user ghi log và job bền vẫn bù; dựng lại là idempotent nên chạy trùng không đổi kết quả.
/// Múi giờ thiếu/không hợp lệ rơi về UTC (cùng chính sách <see cref="UserLocale"/>).
/// </summary>
public static class NightlyWindow
{
    public const int LocalHour = 0;

    public static bool IsDue(string? timeZoneId, DateTimeOffset utcNow)
        => UserLocale.At(timeZoneId, utcNow, DayOfWeek.Monday).Time.Hour == LocalHour;
}
