namespace Application;

/// <summary>
/// Xem trước Edit Goal, không ghi DB: loại goal suy ra, ngân sách khởi điểm, ngày dự kiến, tốc độ tuần/tháng, dải Slow/Standard/Fast.
/// <paramref name="TargetWeightKg"/> null = không đặt đích (giữ cân); <paramref name="WeeklyRatePercent"/> là độ lớn % cân nặng/tuần, null = giữ tốc độ hiện tại hoặc mặc định.
/// </summary>
public sealed record PreviewEditGoalQuery(decimal? TargetWeightKg, decimal? WeeklyRatePercent) : IRequest<GoalEditPreviewResponse>;
