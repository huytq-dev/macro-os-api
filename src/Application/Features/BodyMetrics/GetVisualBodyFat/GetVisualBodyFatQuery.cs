namespace Application;

/// <summary>
/// Trang Visual Body Fat: các lần ghi % mỡ Visual trong khoảng (mới nhất trước, mỗi ngày một dòng), thống kê khoảng (Average, Difference = cuối − đầu) và bucket Day/Week/Month.
/// Tách biệt với % mỡ đo bằng cân (Method Scale, nằm trong cân nặng). Khoảng: <c>To</c> mặc định hôm nay theo múi giờ user; <c>From</c> mặc định 90 ngày trước <c>To</c>, hoặc ngày có dữ liệu đầu tiên khi <c>All</c> = true.
/// </summary>
public sealed record GetVisualBodyFatQuery(DateOnly? From, DateOnly? To, MetricGranularity? Granularity = null, bool? All = null, int? Limit = null) : IRequest<VisualBodyFatResponse>;
