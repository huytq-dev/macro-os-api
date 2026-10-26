namespace Application;

/// <summary>
/// Mark Day Incomplete: chỉ đổi cờ "log chưa đủ" của ngày (expenditure và trung bình check-in bỏ qua ngày này). Đặt true chỉ cho ngày đã có dữ liệu (có entry hoặc nhịn ăn), không thì 400;
/// đặt false luôn được. Không đụng entry, nước, ghi chú, nhịn ăn; không tạo ngày mới. Trả lại ngày sau khi lưu; ngày không tồn tại ⇒ 400 khi đặt true, 404 khi đặt false.
/// </summary>
public sealed record SetDayIncompleteCommand([property: System.Text.Json.Serialization.JsonIgnore] DateOnly Date, bool IsIncomplete) : IRequest<NutritionDataDayModel>;
