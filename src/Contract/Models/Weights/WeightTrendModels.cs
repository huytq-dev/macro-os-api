namespace Contract;

/// <summary>
/// Một ngày của chuỗi trend LIÊN TỤC (mỗi ngày từ lần cân đầu tiên tới hôm nay, kể cả ngày không cân).
/// </summary>
/// <param name="TrendKg">Trend cuối ngày (EMA, 2 chữ số thập phân).</param>
/// <param name="ScaleKg">Cân nặng đo thực của ngày; null nếu hôm đó không cân (trend vẫn có: nội suy hoặc giữ cân cuối).</param>
/// <param name="ChangeKg">Trend hôm nay trừ trend hôm qua (kg, 2 chữ số); null ở ngày đầu tiên. FE làm tròn 0,1 để hiện "↘ −0.1 kg" / "→ No Change".</param>
public sealed record WeightTrendDay(DateOnly Date, decimal TrendKg, decimal? ScaleKg, decimal? ChangeKg);

/// <summary>Một bucket Day/Week/Month của biểu đồ trend: trung bình trend các ngày trong bucket và trung bình các lần cân thực (null nếu không có lần cân nào).</summary>
public sealed record WeightTrendBucket(DateOnly Start, DateOnly End, decimal TrendKg, decimal? ScaleKg, int Days, int ScaleCount);

/// <summary>Trang Weight Trend: chuỗi trend từng ngày trong khoảng + thống kê + bucket.</summary>
/// <param name="TrendDays">Cửa sổ EMA N đang dùng (Algorithm:TrendDays); hệ số làm mượt α = 2 / (N + 1).</param>
/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày cân đầu tiên).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Theo thứ tự thời gian TĂNG dần; chỉ ngày từ lần cân đầu tiên (trước đó chưa có trend) tới To.</param>
/// <param name="Summary">Thống kê trên TREND của các ngày trong khoảng: Average, Difference = trend cuối − trend đầu.</param>
/// <param name="ScaleSummary">Thống kê trên các lần cân thực trong khoảng.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record WeightTrendResponse(
    int TrendDays,
    decimal Alpha,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<WeightTrendDay> Days,
    MetricSummary Summary,
    MetricSummary ScaleSummary,
    IReadOnlyList<WeightTrendBucket> Buckets
);

/// <param name="Days">3 | 7 | 14 | 30 | 90.</param>
/// <param name="ChangeKg">Trend hôm nay trừ trend N ngày trước; null khi chưa đủ dữ liệu (Status = Waiting).</param>
/// <param name="Status">Decrease | Increase | NoChange (|thay đổi| &lt; 0,05 kg) | Waiting.</param>
public sealed record WeightChangeModel(int Days, decimal? ChangeKg, string Status);

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
