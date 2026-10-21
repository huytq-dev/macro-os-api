namespace Contract;

/// <summary>Thống kê một khoảng. Average = trung bình mọi điểm trong khoảng; Difference = điểm cuối trừ điểm đầu (null nếu &lt; 2 điểm); không có điểm → Count 0 và các giá trị null.</summary>
/// <param name="From">Đầu khoảng hiệu lực (khi gửi all=true và không có from: ngày có dữ liệu đầu tiên).</param>
/// <param name="To">Cuối khoảng hiệu lực (mặc định hôm nay theo múi giờ user).</param>
public sealed record MetricSummary(
    DateOnly From,
    DateOnly To,
    int Count,
    decimal? Average,
    decimal? Difference,
    decimal? First,
    decimal? Last,
    decimal? Min,
    decimal? Max
);
