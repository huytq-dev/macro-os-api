namespace Contract;

/// <summary>Insights &amp; Data của trang Weight Trend, tính đến hôm nay theo múi giờ user, từ các lần cân (Scale Weight).</summary>
/// <param name="FirstWeighInDate">Ngày cân đầu tiên; null nếu chưa có cân.</param>
/// <param name="DataDays">Số ngày của chuỗi trend (từ lần cân đầu tới hôm nay).</param>
/// <param name="CurrentWeightKg">Trend hôm nay: ước lượng cân nặng thật sau khi làm mượt.</param>
/// <param name="WeeklyChangeKg">Độ dốc hồi quy tuyến tính của trend 21 ngày gần nhất, kg/tuần (âm = giảm); null nếu có &lt; 7 ngày dữ liệu.</param>
/// <param name="EnergyDeficitKcalPerDay">WeeklyChange × KcalPerKg ÷ 7, kcal/ngày, làm tròn; ÂM khi đang giảm cân (như MacroFactor hiện "−192").</param>
/// <param name="Projected30DaysKg">CurrentWeight + WeeklyChange × 30 ÷ 7: cân nặng dự kiến sau 30 ngày nếu giữ tốc độ hiện tại.</param>
public sealed record WeightTrendInsightsResponse(
    DateOnly Today,
    DateOnly? FirstWeighInDate,
    int DataDays,
    decimal? CurrentWeightKg,
    IReadOnlyList<WeightChangeModel> WeightChanges,
    decimal? WeeklyChangeKg,
    decimal? EnergyDeficitKcalPerDay,
    decimal? Projected30DaysKg,
    int TrendDays,
    int KcalPerKg
);
