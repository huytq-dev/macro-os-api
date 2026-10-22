namespace Contract;

/// <param name="BodyFatPercent">% mỡ đo bằng cân của đúng ngày này (Method Scale/SmartScale); null nếu không có.</param>
/// <param name="RateKgPerWeek">Tốc độ thay đổi trend 7 ngày gần nhất (kg/tuần), âm = giảm.</param>
/// <param name="RateReasonCode">null khi RateKgPerWeek đã tính; INSUFFICIENT_WEIGHT_DATA | STALE_WEIGHT_DATA (cùng cổng dữ liệu và ý nghĩa với Weight Trend Insights): khi khác null, RateKgPerWeek = null (FE hiện "Waiting"), không bao giờ 0.</param>
/// <param name="Summary">Thống kê khoảng: Average, Difference (cuối − đầu), First/Last, Min/Max; luôn có.</param>
/// <param name="Buckets">Trung bình mỗi bucket Day/Week/Month khi gửi granularity; rỗng nếu không.</param>
public sealed record WeightEntriesResponse(
    IReadOnlyList<WeightEntryModel> Items,
    decimal? LatestWeightKg,
    decimal? LatestTrendKg,
    decimal? RateKgPerWeek,
    MetricSummary? Summary = null,
    IReadOnlyList<MetricBucket>? Buckets = null,
    string? RateReasonCode = null
);
