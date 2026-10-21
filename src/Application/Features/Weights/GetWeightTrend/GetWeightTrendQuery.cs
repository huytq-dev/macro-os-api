namespace Application;

/// <summary>
/// Trang Weight Trend: trend MỖI NGÀY từ lần cân đầu tiên (không trước đó) đến To (mặc định và tối đa hôm nay theo múi giờ user), kèm thống kê trên trend (Average, Difference) và trên số cân thực,
/// và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: từ ngày cân đầu tiên. Trend luôn tính từ lần cân đầu tiên của user (EMA cần cả lịch sử), chỉ phần xuất ra bị cắt theo khoảng.
/// </summary>
public sealed record GetWeightTrendQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<WeightTrendResponse>;
