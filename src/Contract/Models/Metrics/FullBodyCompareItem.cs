namespace Contract;

/// <summary>Một chỉ số trong so sánh hai ngày. Delta = ValueB − ValueA (null nếu thiếu một bên).</summary>
/// <param name="Key">scaleWeight | trendWeight | visualBodyFat | khoá số đo vòng (neck, waist, leftBicep…).</param>
/// <param name="Group">WeightAndBodyFat | UpperBody | Arms | Legs.</param>
/// <param name="Trend">Up | Down | Same; null khi không có Delta.</param>
/// <param name="Quality">Good | Bad | Neutral theo mục tiêu của user (giảm cân: giảm là Good; tăng cân/tăng cơ: tăng là Good; còn lại Neutral; % mỡ giảm luôn Good); null khi không có Delta.</param>
public sealed record FullBodyCompareItem(string Key, string Group, decimal? ValueA, decimal? ValueB, decimal? Delta, string? Trend, string? Quality);
