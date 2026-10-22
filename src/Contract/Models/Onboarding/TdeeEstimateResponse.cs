namespace Contract;

/// <summary>TDEE khởi điểm ước tính từ câu trả lời Basics.</summary>
/// <param name="Method">MifflinStJeor (dự phòng khi chưa chọn tỉ lệ mỡ), hoặc Cunningham khi hồ sơ có tỉ lệ mỡ (BMR = 500 + 22 × lean mass).</param>
/// <param name="BodyFatPercent">% mỡ (giữa khoảng đã chọn) mà công thức Cunningham đã dùng; null khi dùng Mifflin-St Jeor.</param>
public sealed record TdeeEstimateResponse(int TdeeKcal, int BmrKcal, string Method, decimal WeightKg, decimal? BodyFatPercent = null);
