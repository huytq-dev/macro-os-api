namespace Contract;

/// <param name="Status">Calibrating | Updating | Holding. FE: Updating = vòng tròn trên đường; Holding và Calibrating (số không đang cập nhật) = ô vuông.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range. LUÔN null cho tới khi Flux được kích hoạt bằng chính sách đã duyệt (dải ± hằng số cũ đã gỡ).</param>
/// <param name="FluxHighKcal">Cận trên Flux Range; luôn null như trên.</param>
public sealed record ExpenditurePointModel(
    DateOnly SnapshotDate,
    decimal EstimatedTdeeKcal,
    string Status,
    decimal AvgCaloriesConsumed,
    decimal? WeightChangeRateKgPerWeek,
    int WindowDays,
    decimal? FluxLowKcal,
    decimal? FluxHighKcal,
    bool IsCalibrating = false
);
