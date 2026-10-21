namespace Contract;

/// <summary>Bucket Day/Week/Month: trung bình các ngày đã log hợp lệ trong bucket; chỉ có bucket chứa ít nhất một ngày như vậy.</summary>
public sealed record NutrientSeriesBucket(DateOnly Start, DateOnly End, int LoggedDays, decimal Average, decimal? AverageTarget);
