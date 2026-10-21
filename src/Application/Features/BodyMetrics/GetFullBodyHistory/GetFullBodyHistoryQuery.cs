namespace Application;

/// <summary>
/// Danh sách các ngày có dữ liệu Full Body (mới nhất trước) kèm số chỉ số, % mỡ Visual, số đo vòng, cân và trend của ngày đó. From/To tuỳ chọn (gồm hai đầu), mặc định mọi ngày.
/// Chỉ trả ngày có ít nhất một chỉ số; hàng đã xoá mềm bị loại.
/// </summary>
public sealed record GetFullBodyHistoryQuery(DateOnly? From, DateOnly? To) : IRequest<FullBodyHistoryResponse>;
