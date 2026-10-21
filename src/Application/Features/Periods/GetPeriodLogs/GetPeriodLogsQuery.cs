namespace Application;

/// <summary>
/// UC-E05: các kỳ kinh (mới nhất trước) có StartDate trong [From, To] (mặc định 365 ngày gần nhất), kèm Days = các ngày đã theo dõi trong khoảng (trải từ mọi kỳ giao với khoảng, kể cả kỳ bắt đầu trước From)
/// và LastPeriodLengthDays. Khoảng tối đa 3660 ngày (400 nếu vượt).
/// </summary>
public sealed record GetPeriodLogsQuery(DateOnly? From, DateOnly? To) : IRequest<PeriodLogsResponse>;
