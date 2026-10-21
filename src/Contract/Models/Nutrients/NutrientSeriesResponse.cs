namespace Contract;

/// <param name="GoalKind">Loại mục tiêu hiệu lực tại <c>To</c> (Range | Minimum | Maximum | Target | TargetWithCeiling | None, xem <c>NutrientModel.GoalKind</c>); Calories/P/F/C = Target (None khi chưa có program).</param>
/// <param name="Days">Thời gian TĂNG dần, LIÊN TỤC từ From đến To (ngày không có dữ liệu có Amount null nhưng vẫn có Target/Floor/Ceiling nếu có).</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record NutrientSeriesResponse(
    string Code,
    string Name,
    string Unit,
    int DisplayPrecision,
    string GoalKind,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<NutrientSeriesDay> Days,
    NutrientSeriesSummary Summary,
    IReadOnlyList<NutrientSeriesBucket> Buckets
);
