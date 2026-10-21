namespace Application;

/// <summary>UC-H02/H05: DailyProgressSnapshot trong khoảng ngày (mặc định 30 ngày).</summary>
public sealed record GetDailyProgressQuery(DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<DailyProgressModel>>;
