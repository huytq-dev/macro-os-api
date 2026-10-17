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

/// <param name="Days">Mốc so sánh: 3, 7, 14, 30 hoặc 90 ngày trước.</param>
/// <param name="DeltaKcal">TDEE hiện tại − TDEE tại mốc; null khi Waiting.</param>
/// <param name="Status">Ready | Waiting (chưa có điểm ở mốc đó).</param>
public sealed record ExpenditureChangeModel(int Days, decimal? DeltaKcal, string Status)
{
    public const string Ready = "Ready";
    public const string Waiting = "Waiting";
}

/// <param name="HoldingReason">Lý do Holding của điểm mới nhất; null nếu không Holding.</param>
public sealed record ExpenditureResponse(
    ExpenditurePointModel? Current,
    string? HoldingReason,
    DateOnly? ExpenditureStartDate,
    IReadOnlyList<ExpenditurePointModel> Points
);

/// <param name="IsLogged">Ngày có FoodLog; false = chưa log (UI hiện –/N/A), khác với log 0 kcal.</param>
/// <param name="EstimatedTdeeKcal">TDEE ước lượng cùng ngày (Energy Balance chế độ Expenditure); null nếu chưa có snapshot.</param>
public sealed record DailyProgressModel(
    DateOnly SnapshotDate,
    bool IsLogged,
    decimal CaloriesConsumed,
    decimal CaloriesTarget,
    decimal ProteinConsumed,
    decimal CarbsConsumed,
    decimal FatConsumed,
    decimal? TrendWeightKg,
    Guid? MacroProgramWeekId,
    decimal? EstimatedTdeeKcal
);
