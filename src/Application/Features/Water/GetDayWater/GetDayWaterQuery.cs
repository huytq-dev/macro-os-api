namespace Application;

/// <summary>WP2: các lần uống nước của một ngày (mặc định hôm nay), theo giờ.</summary>
public sealed record GetDayWaterQuery(DateOnly? Date) : IRequest<DayWaterResponse>;
