namespace Application;

/// <summary>
/// UC-F05: biến thiên TDEE hiện tại so với 3/7/14/30/90 ngày trước. Waiting (DeltaKcal null) khi chưa có điểm ở mốc đó
/// (mốc trước ExpenditureStartDate hoặc chưa đủ lịch sử). BE tính vì FE chỉ giữ điểm của kỳ đang xem (1W không đủ 90 ngày).
/// </summary>
public sealed record GetExpenditureChangesQuery : IRequest<IReadOnlyList<ExpenditureChangeModel>>;
