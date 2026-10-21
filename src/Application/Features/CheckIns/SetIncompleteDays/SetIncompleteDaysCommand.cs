namespace Application;

/// <summary>
/// Đặt tập ngày "log chưa đủ" trong 7 ngày gần nhất (có trong <see cref="Dates"/> = đánh dấu, không có = bỏ đánh dấu) → idempotent.
/// Expenditure được dựng lại đồng bộ (ngày đánh dấu bị bỏ qua) rồi trả session mới. Ngày chưa có log bị bỏ qua.
/// </summary>
public sealed record SetIncompleteDaysCommand(List<DateOnly> Dates) : IRequest<CheckInSessionResponse>;
