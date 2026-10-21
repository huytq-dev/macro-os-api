namespace Contract;

/// <param name="Status">Calibrating | Updating | Holding. FE: Updating = vòng tròn trên đường; Holding và Calibrating (số không đang cập nhật) = ô vuông.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range = TDEE − nửa bề rộng (độ bất định của ước lượng, rộng hơn khi ít dữ liệu).</param>
/// <param name="FluxHighKcal">Cận trên Flux Range = TDEE + nửa bề rộng.</param>
public sealed record ExpenditurePointModel(
    DateOnly SnapshotDate,
    decimal EstimatedTdeeKcal,
    string Status,
    decimal AvgCaloriesConsumed,
    decimal? WeightChangeRateKgPerWeek,
    int WindowDays,
    decimal? FluxLowKcal,
    decimal? FluxHighKcal
);
