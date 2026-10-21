namespace Application;

/// <summary>
/// Lưu cả sheet trong MỘT lần lưu (một transaction), thay thế trạng thái của ngày: <paramref name="VisualBodyFatPercent"/> có giá trị → hàng Visual,
/// null → xoá hàng Visual; <paramref name="Tape"/> có ít nhất một ô → hàng Tape (ô null bị xoá), không có → xoá hàng Tape. Idempotent.
/// </summary>
public sealed record UpsertFullBodyCommand(DateOnly? Date, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape) : IRequest<FullBodyResponse>;
