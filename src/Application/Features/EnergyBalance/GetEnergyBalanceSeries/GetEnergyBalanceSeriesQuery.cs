namespace Application;

/// <summary>
/// Trang Energy Balance (hai tab Expenditure / Calorie Targets): mỗi ngày LIÊN TỤC từ From đến To (mặc định và tối đa hôm nay theo múi giờ user) với kcal ăn, expenditure và target của ngày đó,
/// chênh lệch, thống kê khoảng (trung bình mỗi ngày đã log) và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: từ ngày có dữ liệu đầu tiên.
/// </summary>
public sealed record GetEnergyBalanceSeriesQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<EnergyBalanceSeriesResponse>;
