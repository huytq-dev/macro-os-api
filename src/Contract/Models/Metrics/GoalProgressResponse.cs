namespace Contract;

/// <param name="EstimatedCompletionDate">Ngày dự kiến chạm target, NEO vào ngày lần cân thật cuối (không trôi theo hôm nay); chỉ có khi <c>EstimatedCompletionStatus = Estimated</c>, luôn ≥ hôm nay. Ngoại suy từ tốc độ 14 ngày gần nhất (công thức cũ, chưa phải canonical weekly rate).</param>
/// <param name="EstimatedCompletionStatus">Estimated | GoalAchieved (đã đạt Goal Weight: KHÔNG có ngày ETA giả) | Unavailable (xem reason code) | NotApplicable (Maintenance). null chỉ khi client/BE cũ chưa gửi.</param>
/// <param name="EstimatedCompletionReasonCode">Khi Unavailable: INSUFFICIENT_WEIGHT_DATA | STALE_WEIGHT_DATA (cổng TRA, guard khoảng trống 14 ngày, dữ liệu cũ; cùng mã với Weight Trend) | NO_TARGET_WEIGHT | RATE_NOT_TOWARD_GOAL (tốc độ 0 hoặc sai hướng) | ETA_IN_PAST | ETA_TOO_FAR. Còn lại null.</param>
public sealed record GoalProgressResponse(
    string GoalType,
    DateOnly StartDate,
    decimal? StartWeightKg,
    decimal? TargetWeightKg,
    decimal? CurrentTrendKg,
    decimal? ProgressPercent,
    decimal? DeviationKg,
    DateOnly? EstimatedCompletionDate,
    IReadOnlyList<GoalWeekModel> Weeks,
    string? EstimatedCompletionStatus = null,
    string? EstimatedCompletionReasonCode = null
);
