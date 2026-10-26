namespace Application;

/// <summary>
/// Mark Day Fasting: chỉ đổi cờ nhịn ăn của ngày. Bật chỉ được khi ngày KHÔNG có entry nào: có entry ⇒ 409 FASTING_NEEDS_EMPTY_DAY; có manual dương (calories hay macro) ⇒ 400 FASTING_WITH_MANUAL_NUTRITION; không đổi gì.
/// Manual 0 được phép (nhịn ăn 0 kcal hợp lệ). Không đụng entry, nước, ghi chú, override. Ngày không ở tương lai theo múi giờ user. Bỏ nhịn ăn trên ngày trống thật thì cờ chưa-đủ cũ tự bỏ (R2-4). Trả lại ngày sau khi lưu.
/// </summary>
public sealed record SetDayFastingCommand([property: System.Text.Json.Serialization.JsonIgnore] DateOnly Date, bool IsFasting) : IRequest<NutritionDataDayModel>;
