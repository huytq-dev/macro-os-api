namespace Application;

/// <summary>
/// Trang Expenditure: TDEE MỖI NGÀY từ snapshot đầu tiên đến To (mặc định và tối đa hôm nay theo múi giờ user; ngày chưa có snapshot giữ TDEE gần nhất), kèm Flux Range,
/// thay đổi so với hôm trước, thống kê (Average, Difference) và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: từ ngày đầu tiên của chuỗi.
/// </summary>
public sealed record GetExpenditureSeriesQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<ExpenditureSeriesResponse>;
