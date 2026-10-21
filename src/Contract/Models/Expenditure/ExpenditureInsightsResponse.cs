namespace Contract;

/// <summary>Insights &amp; Data của trang Expenditure, tính đến hôm nay theo múi giờ user.</summary>
/// <param name="CurrentKcal">TDEE hôm nay; null nếu chưa có snapshot.</param>
/// <param name="Strategy">Calibrating | Updating | Holding: trạng thái hôm nay (null nếu chưa có snapshot).</param>
/// <param name="HoldingReason">Lý do Holding dạng câu (null nếu không Holding hoặc không có lý do cụ thể).</param>
/// <param name="HoldingReasonCodes">MissingLogs (&gt; 3/7 ngày gần nhất không có log hợp lệ) | NoRecentWeighIn (không cân lần nào trong 7 ngày); rỗng nếu không Holding.</param>
/// <param name="CalibratingDaysLeft">Số ngày còn lại của giai đoạn Calibrating; null khi không Calibrating.</param>
/// <param name="LoggedDaysLast7">Số ngày trong 7 ngày gần nhất có log hợp lệ (có entry hoặc nhịn ăn, không đánh dấu chưa đủ).</param>
/// <param name="WeighInDaysLast7">Số ngày trong 7 ngày gần nhất có cân.</param>
public sealed record ExpenditureInsightsResponse(
    DateOnly Today,
    DateOnly? FirstDate,
    decimal? CurrentKcal,
    string? Strategy,
    string? HoldingReason,
    IReadOnlyList<string> HoldingReasonCodes,
    int? CalibratingDaysLeft,
    int LoggedDaysLast7,
    int WeighInDaysLast7,
    IReadOnlyList<ExpenditureInsightChange> Changes
);
