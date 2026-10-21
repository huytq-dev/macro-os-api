namespace Contract;

/// <summary>Trang Expenditure: chuỗi TDEE mỗi ngày trong khoảng + thống kê + bucket.</summary>
/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày đầu tiên của chuỗi).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Thời gian TĂNG dần, liên tục; rỗng nếu chưa có snapshot nào.</param>
/// <param name="Summary">Thống kê trên ExpenditureKcal các ngày trong khoảng: Average, Difference = ngày cuối − ngày đầu.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record ExpenditureSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ExpenditureSeriesDay> Days,
    MetricSummary Summary,
    IReadOnlyList<MetricBucket> Buckets
);
