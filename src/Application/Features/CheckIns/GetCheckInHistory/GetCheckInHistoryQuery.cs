namespace Application;

/// <summary>Lịch sử check-in, mới nhất trước.</summary>
public sealed record GetCheckInHistoryQuery : IRequest<IReadOnlyList<CheckInHistoryModel>>;
