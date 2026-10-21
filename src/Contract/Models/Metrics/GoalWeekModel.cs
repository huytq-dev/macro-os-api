namespace Contract;

// --- Goal progress ---

/// <param name="TrendKg">Trend cuối tuần (tuần = 7 ngày kể từ StartDate); null nếu tuần không có cân.</param>
/// <param name="DeltaKg">Chênh so với tuần trước có dữ liệu (tuần đầu so với StartWeightKg); âm = giảm.</param>
public sealed record GoalWeekModel(DateOnly WeekStart, decimal? TrendKg, decimal? DeltaKg);
