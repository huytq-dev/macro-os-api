namespace Contract;

/// <param name="Items">Mới nhất trước, mỗi ngày một giá trị (có thể bị cắt bởi limit).</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
/// <param name="LatestCm">Lần đo mới nhất (cả ngoài khoảng); null nếu chưa có.</param>
public sealed record MeasurementSeriesResponse(string Key, string Group, IReadOnlyList<MeasurementEntryModel> Items, MetricSummary Summary, IReadOnlyList<MetricBucket> Buckets, decimal? LatestCm);
