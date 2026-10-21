namespace Application;

/// <summary>UC-D01: log của một ngày (mặc định hôm nay) + tổng + target tuần hiệu lực.</summary>
public sealed record GetDayLogQuery(DateOnly? Date) : IRequest<DayLogResponse>;
