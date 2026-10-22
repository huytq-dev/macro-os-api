namespace Contract;

/// <summary>Bucket Day/Week/Month: trung bình các ngày đã log hợp lệ trong bucket; chỉ có bucket chứa ít nhất một ngày như vậy. AverageTarget chỉ tính ngày có target VERIFIED (hoặc không phụ thuộc program); UnverifiedTargetDays = số ngày hợp lệ bị loại vì target LEGACY_UNVERIFIED.</summary>
public sealed record NutrientSeriesBucket(DateOnly Start, DateOnly End, int LoggedDays, decimal Average, decimal? AverageTarget, int UnverifiedTargetDays = 0);
