namespace Contract;

/// <param name="MetricCount">Số chỉ số có giá trị của ngày: % mỡ Visual (nếu có) + số ô số đo vòng có giá trị.</param>
/// <param name="Tape">null khi ngày đó không có số đo vòng.</param>
public sealed record FullBodyHistoryItem(DateOnly Date, int MetricCount, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape, decimal? ScaleWeightKg, decimal? TrendWeightKg);
