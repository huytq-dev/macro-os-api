namespace Application;

/// <summary>
/// Clear Food Log Day: xoá mềm các MÓN đã log của ngày (mọi entry, kể cả Quick Add). Giữ nguyên dòng FoodLog, Manual Nutrition (override), nước, ghi chú, cờ nhịn ăn; cờ chưa-đủ chỉ tự bỏ khi ngày thành trống thật (R2-4).
/// 404 nếu user không có ngày đó; ngày không có món ⇒ không làm gì (thành công). Trước đây xoá cứng cả dòng FoodLog (kéo theo nước, ghi chú, cờ).
/// </summary>
public sealed record ClearDayLogCommand(DateOnly Date) : IRequest<Unit>;
