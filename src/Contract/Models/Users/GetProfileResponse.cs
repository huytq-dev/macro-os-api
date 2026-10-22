namespace Contract;

/// <param name="CreatedAt">Ngày tạo tài khoản (User.CreatedAt) — FE hiện "Member Since".</param>
/// <param name="EnergyUnit">Kcal | Kj — đơn vị hiển thị năng lượng của user (chỉ là cài đặt hiển thị, mọi số liệu BE vẫn là kcal).</param>
/// <param name="EffectiveTimeZone">Múi giờ thực sự dùng cho mọi ngày nghiệp vụ (id hồ sơ nếu hợp lệ, ngược lại "UTC").</param>
/// <param name="TimeZoneFallback">null = dùng đúng múi giờ hồ sơ; "Missing" (chưa đặt) | "Invalid" (id không tồn tại): đang dự phòng UTC, FE nên nhắc user chọn múi giờ (KHÔNG tự đổi bằng múi giờ máy).</param>
/// <param name="WeekStartDay">Monday…Sunday — ngày đầu tuần (tên <see cref="DayOfWeek"/>); mặc định Monday.</param>
public sealed record GetProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string Gender,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    string ActivityLevel,
    string UnitPreference,
    string? Bio,
    string? TimeZone,
    string? Locale,
    bool EmailVerified,
    DateTimeOffset CreatedAt,
    string EnergyUnit,
    string WeekStartDay,
    string? EffectiveTimeZone = null,
    string? TimeZoneFallback = null
);
