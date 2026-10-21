namespace Contract;

/// <summary>
/// Một ngày của chuỗi trend LIÊN TỤC (mỗi ngày từ lần cân đầu tiên tới hôm nay, kể cả ngày không cân).
/// </summary>
/// <param name="TrendKg">Trend cuối ngày (EMA, 2 chữ số thập phân).</param>
/// <param name="ScaleKg">Cân nặng đo thực của ngày; null nếu hôm đó không cân (trend vẫn có: nội suy hoặc giữ cân cuối).</param>
/// <param name="ChangeKg">Trend hôm nay trừ trend hôm qua (kg, 2 chữ số); null ở ngày đầu tiên. FE làm tròn 0,1 để hiện "↘ −0.1 kg" / "→ No Change".</param>
public sealed record WeightTrendDay(DateOnly Date, decimal TrendKg, decimal? ScaleKg, decimal? ChangeKg);
