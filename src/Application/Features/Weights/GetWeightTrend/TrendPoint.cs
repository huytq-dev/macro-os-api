namespace Application;

/// <summary>Một ngày của chuỗi trend liên tục ở độ chính xác đầy đủ (chưa làm tròn).</summary>
public readonly record struct TrendPoint(DateOnly Date, decimal Trend, decimal? Scale);
