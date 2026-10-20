namespace Application;

/// <summary>
/// UC-E03 / trang Scale Weight: danh sách cân (mới nhất trước) trong khoảng kèm % mỡ cân của từng ngày, thống kê khoảng (Average, Difference) và bucket Day/Week/Month.
/// To mặc định hôm nay theo múi giờ user; From mặc định 90 ngày trước To, hoặc ngày có cân đầu tiên khi All = true. Granularity null = không dựng bucket. Limit chỉ cắt danh sách Items (Summary/Buckets vẫn tính trên cả khoảng), vd thẻ Dashboard "last 7 entries".
/// </summary>
public sealed record GetWeightEntriesQuery(DateOnly? From, DateOnly? To, MetricGranularity? Granularity = null, bool? All = null, int? Limit = null) : IRequest<WeightEntriesResponse>;
