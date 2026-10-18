namespace Contract;

// --- Steps ---

public sealed record StepEntryModel(Guid Id, DateOnly LogDate, int Steps, string Source);

/// <param name="Last7DayAverage">Trung bình bước/ngày của các ngày có dữ liệu trong 7 ngày cuối khoảng hỏi; null nếu không có.</param>
public sealed record StepEntriesResponse(IReadOnlyList<StepEntryModel> Items, int? Last7DayAverage);

// --- Period ---

public sealed record PeriodLogModel(
    Guid Id,
    DateOnly StartDate,
    DateOnly? EndDate,
    int? CycleLengthDays,
    int? PeriodLengthDays,
    string? Symptoms,
    string? Notes
);

/// <param name="DaysSinceLastStart">Số ngày từ StartDate kỳ gần nhất tới hôm nay; null nếu chưa có kỳ nào.</param>
public sealed record PeriodLogsResponse(IReadOnlyList<PeriodLogModel> Items, DateOnly? LastStartDate, int? DaysSinceLastStart);

// --- Body fat ---

public sealed record BodyFatEntryModel(Guid Id, DateOnly MeasuredDate, string Method, decimal BodyFatPercent, string? Notes);

public sealed record BodyFatResponse(IReadOnlyList<BodyFatEntryModel> Items, decimal? LatestPercent);

// --- Goal progress ---

/// <param name="TrendKg">Trend cuối tuần (tuần = 7 ngày kể từ StartDate); null nếu tuần không có cân.</param>
/// <param name="DeltaKg">Chênh so với tuần trước có dữ liệu (tuần đầu so với StartWeightKg); âm = giảm.</param>
public sealed record GoalWeekModel(DateOnly WeekStart, decimal? TrendKg, decimal? DeltaKg);

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

public sealed record GoalProgressResponse(
    string GoalType,
    DateOnly StartDate,
    decimal? StartWeightKg,
    decimal? TargetWeightKg,
    decimal? CurrentTrendKg,
    decimal? ProgressPercent,
    decimal? DeviationKg,
    DateOnly? EstimatedCompletionDate,
    IReadOnlyList<GoalWeekModel> Weeks
);
