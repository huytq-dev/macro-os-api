namespace Contract;

/// <summary>Một bucket Day/Week/Month của biểu đồ trend: trung bình trend các ngày trong bucket và trung bình các lần cân thực (null nếu không có lần cân nào).</summary>
public sealed record WeightTrendBucket(DateOnly Start, DateOnly End, decimal TrendKg, decimal? ScaleKg, int Days, int ScaleCount);
