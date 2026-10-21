namespace Application;

/// <summary>
/// Trang chi tiết nutrient — biểu đồ lịch sử: mỗi ngày LIÊN TỤC từ From đến To (mặc định và tối đa hôm nay theo múi giờ user) với lượng đã ăn, target CỦA NGÀY ĐÓ và dải Range (floor→ceiling),
/// thống kê khoảng (trung bình mỗi ngày đã log) và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: Calories/P/F/C từ ngày sớm hơn giữa log đầu tiên và tuần program đầu tiên, vi chất từ ngày log đầu tiên. Mã không biết → 404.
/// </summary>
public sealed record GetNutrientSeriesQuery(string Code, DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<NutrientSeriesResponse>;
