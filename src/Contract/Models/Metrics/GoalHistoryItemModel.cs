namespace Contract;

/// <param name="ProgressPercent">Lose/Gain: 0–100; Maintenance: null.</param>
/// <param name="DeviationKg">Maintenance: trend hiện tại trừ target; ngược lại null.</param>
/// <param name="EstimatedCompletionDate">Ngoại suy từ tốc độ 14 ngày gần nhất; null nếu chưa đủ dữ liệu hoặc đang đi sai hướng.</param>
/// <summary>Một goal trong lịch sử (mới nhất trước). RatePerWeekKg âm = giảm, dương = tăng; EndDate null = đang chạy.</summary>
/// <param name="Status">Active | Achieved | Superseded | Cancelled (tên GoalStatus).</param>
public sealed record GoalHistoryItemModel(
    Guid Id,
    string GoalType,
    decimal? TargetWeightKg,
    decimal? StartWeightKg,
    decimal? RatePerWeekKg,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status
);
