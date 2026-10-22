namespace Contract;

/// <summary>Insights &amp; Data của trang Expenditure, tính đến hôm nay theo múi giờ user.</summary>
/// <param name="CurrentKcal">TDEE hôm nay; null nếu chưa có snapshot.</param>
/// <param name="Strategy">Calibrating | Updating | Holding: trạng thái thuật toán hôm nay (Calibrating = chuỗi mới chưa từng Updating) (null nếu chưa có snapshot; dữ liệu cũ chưa dựng lại có thể còn "Calibrating").</param>
/// <param name="HoldingReason">Lý do Holding dạng câu (null nếu không Holding hoặc không có lý do cụ thể).</param>
/// <param name="HoldingReasonCodes">MissingLogs (chưa đủ cả 4/7 ngày log lẫn 3 ngày liên tiếp) | RecoveringLogs (đang có 1–2 ngày hợp lệ liên tiếp, chưa đủ 3) | NoRecentWeighIn (không cân lần nào trong 7 ngày); rỗng nếu không Holding.</param>
/// <param name="CalibratingDaysLeft">DEPRECATED, luôn null: không còn đếm ngược (hết hiệu chỉnh phụ thuộc dữ liệu, không phải ngày chắc chắn). FE hiện "đang thu thập dữ liệu" khi IsCalibrating.</param>
/// <param name="LoggedDaysLast7">Số ngày trong 7 ngày gần nhất có log hợp lệ (nhịn ăn, hoặc có entry với tổng &gt; 0 kcal; không đánh dấu chưa đủ).</param>
/// <param name="WeighInDaysLast7">Số ngày trong 7 ngày gần nhất có cân.</param>
/// <param name="MinLoggedDaysPer7">Số ngày tối thiểu trong 7 ngày gần nhất phải có log kcal hợp lệ để expenditure được Updating (BE là nguồn sự thật; FE không hardcode ngưỡng).</param>
/// <param name="MinWeighInDaysPer7">Số lần cân tối thiểu trong 7 ngày gần nhất để Updating.</param>
/// <param name="IsCalibrating">Nhãn UI giai đoạn ước lượng ban đầu (&lt; CalibratingDays kể từ ExpenditureStartDate); tách khỏi Strategy, không đóng băng TDEE.</param>
/// <param name="ResumeInDays">Khi Holding và đã có cân trong 7 ngày: số ngày log hợp lệ liên tiếp còn thiếu để resume NẾU user tiếp tục log hợp lệ mỗi ngày (ước tính, không phải cam kết); null khi không áp dụng.</param>
/// <param name="ResumeConsecutiveDays">Số ngày hợp lệ liên tiếp để resume sau Holding (BE là nguồn sự thật; MF xác nhận cơ chế 3 ngày, định nghĩa chi tiết là chính sách MacroOS).</param>
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
    IReadOnlyList<ExpenditureInsightChange> Changes,
    bool IsCalibrating = false,
    int MinLoggedDaysPer7 = 4,
    int MinWeighInDaysPer7 = 1,
    string AlgorithmName = ExpenditureLabels.AlgorithmName,
    int? ResumeInDays = null,
    int ResumeConsecutiveDays = 3
);
