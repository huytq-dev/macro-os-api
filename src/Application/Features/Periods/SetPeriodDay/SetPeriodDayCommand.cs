namespace Application;

/// <summary>
/// Bật/tắt MỘT ngày kinh (sheet "Are you on your period?" của MacroFactor), idempotent: bật ngày đã theo dõi / tắt ngày chưa theo dõi = không đổi.
/// Bật → kéo dài kỳ sát ngày đó, gộp hai kỳ nếu lấp đúng khe 1 ngày, hoặc tạo kỳ [ngày, ngày]. Tắt → xoá kỳ 1 ngày, thu hẹp đầu/cuối, hoặc tách đôi khi ở giữa. Tất cả trong MỘT lần lưu (nguyên tử);
/// độ dài chu kỳ / kỳ tính lại cả chuỗi. Ngày ở tương lai (theo múi giờ user) → 400. Ngày lấy từ route (JsonIgnore, nullable để model binding không đòi trong body).
/// </summary>
public sealed record SetPeriodDayCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] DateOnly? Date,
    bool? IsOnPeriod
) : IRequest<Unit>;
