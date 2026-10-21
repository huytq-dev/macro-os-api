namespace Contract;

/// <summary>Trang Weight Trend: chuỗi trend từng ngày trong khoảng + thống kê + bucket.</summary>
/// <param name="TrendDays">Cửa sổ EMA N đang dùng (Algorithm:TrendDays); hệ số làm mượt α = 2 / (N + 1).</param>
/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày cân đầu tiên).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Theo thứ tự thời gian TĂNG dần; chỉ ngày từ lần cân đầu tiên (trước đó chưa có trend) tới To.</param>
/// <param name="Summary">Thống kê trên TREND của các ngày trong khoảng: Average, Difference = trend cuối − trend đầu.</param>
/// <param name="ScaleSummary">Thống kê trên các lần cân thực trong khoảng.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record WeightTrendResponse(
    int TrendDays,
    decimal Alpha,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<WeightTrendDay> Days,
    MetricSummary Summary,
    MetricSummary ScaleSummary,
    IReadOnlyList<WeightTrendBucket> Buckets
);
