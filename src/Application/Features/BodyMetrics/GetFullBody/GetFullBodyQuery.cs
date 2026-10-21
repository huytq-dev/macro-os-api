namespace Application;

/// <summary>Sheet Metrics (Full Body) của một ngày (mặc định hôm nay) để điền sẵn.</summary>
public sealed record GetFullBodyQuery(DateOnly? Date) : IRequest<FullBodyResponse>;
