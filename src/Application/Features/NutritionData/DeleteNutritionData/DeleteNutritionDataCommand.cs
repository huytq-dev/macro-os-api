namespace Application;

/// <summary>
/// Delete Nutrition Data (NDM): xoá DỮ LIỆU NDM của ngày = manual override (4 cột) và cờ nhịn ăn. Giữ nguyên dòng FoodLog, món đã log (kể cả Quick Add cũ), nước, ghi chú; cờ chưa-đủ chỉ tự bỏ khi ngày thành trống thật (R2-4).
/// Ngày chỉ có món (không có override, không nhịn ăn): 400, không xoá gì — xoá món ở Food Log. 404 nếu user không có ngày đó; ngày trống ⇒ không làm gì.
/// Trước đây xoá cứng cả dòng FoodLog (kéo theo nước, ghi chú, cờ).
/// </summary>
public sealed record DeleteNutritionDataCommand(DateOnly Date) : IRequest<Unit>;
