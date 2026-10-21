namespace Contract;

/// <param name="Items">Mới nhất trước, mỗi ngày một dòng.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
/// <param name="LatestPercent">Lần ghi mới nhất trong khoảng; null nếu khoảng rỗng.</param>
public sealed record VisualBodyFatResponse(
    IReadOnlyList<VisualBodyFatEntryModel> Items,
    MetricSummary Summary,
    IReadOnlyList<MetricBucket> Buckets,
    decimal? LatestPercent
);
