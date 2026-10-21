namespace Contract;

/// <summary>
/// Sheet Metrics (Full Body) của một ngày: % mỡ Visual (hàng <c>Method=Visual</c>) + số đo vòng (hàng <c>Method=Tape</c>).
/// </summary>
/// <param name="Tape">null khi ngày đó chưa có số đo vòng nào.</param>
/// <param name="HasData">false khi cả % mỡ lẫn số đo đều trống.</param>
/// <param name="ScaleWeightKg">Cân nặng của đúng ngày này nếu có (không tính vào HasData).</param>
/// <param name="TrendWeightKg">Trend cân nặng của ngày này nếu tính được.</param>
public sealed record FullBodyResponse(DateOnly Date, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape, bool HasData, decimal? ScaleWeightKg = null, decimal? TrendWeightKg = null);
