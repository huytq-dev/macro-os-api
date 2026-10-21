namespace Contract;

/// <summary>TDEE khởi điểm ước tính từ câu trả lời Basics.</summary>
/// <param name="Method">MifflinStJeor, hoặc KatchMcArdle khi đã chọn tỉ lệ mỡ.</param>
public sealed record TdeeEstimateResponse(int TdeeKcal, int BmrKcal, string Method, decimal WeightKg);
