namespace Contract;

/// <summary>Một bucket của biểu đồ: Start..End (Day: một ngày; Week: 7 ngày từ ngày đầu tuần của user; Month: cả tháng), chỉ có bucket chứa dữ liệu.</summary>
public sealed record MetricBucket(DateOnly Start, DateOnly End, int Count, decimal Average);
