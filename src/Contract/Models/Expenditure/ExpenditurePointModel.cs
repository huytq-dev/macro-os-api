namespace Contract;

/// <param name="Status">Calibrating | Updating | Holding. FE: Updating = vòng tròn trên đường; Holding và Calibrating (số không đang cập nhật) = ô vuông.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range (kcal nguyên, MACROOS_FLUX_V1); <c>FluxLowKcal ≤ EstimatedTdeeKcal ≤ FluxHighKcal</c> khi có dải; null khi Calibrating hoặc chưa có quan sát hợp lệ (xem ExpenditureSeriesDay).</param>
/// <param name="FluxHighKcal">Cận trên Flux Range; cùng quy tắc.</param>
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
