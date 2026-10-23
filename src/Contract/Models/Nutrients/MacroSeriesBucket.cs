namespace Contract;

/// <summary>Bucket Day/Week/Month (tuần bắt đầu theo hồ sơ user): trung bình mỗi ngày đã log trong bucket; chỉ có bucket chứa ít nhất một ngày đã log hợp lệ.</summary>
public sealed record MacroSeriesBucket(DateOnly Start, DateOnly End, int LoggedDays, MacroAverages Averages);
