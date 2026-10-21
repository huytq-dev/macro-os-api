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

/// <summary>
/// Một ngày của chuỗi expenditure LIÊN TỤC (mỗi ngày từ ngày có dữ liệu đầu tiên tới hôm nay, kể cả ngày chưa có snapshot: giữ TDEE gần nhất).
/// </summary>
/// <param name="ExpenditureKcal">TDEE cuối ngày, làm tròn kcal.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range = TDEE − nửa bề rộng (rộng hơn khi cửa sổ dữ liệu ngắn).</param>
/// <param name="FluxHighKcal">Cận trên Flux Range.</param>
/// <param name="Status">Calibrating | Updating | Holding. Ngày sau snapshot cuối (chưa có dữ liệu mới) giữ TDEE nên là Holding, hoặc Calibrating nếu còn trong giai đoạn học. FE: Updating = vòng tròn; Holding/Calibrating = ô vuông.</param>
/// <param name="ChangeKcal">TDEE hôm nay trừ hôm qua, làm tròn kcal; null ở ngày đầu tiên của chuỗi.</param>
public sealed record ExpenditureSeriesDay(DateOnly Date, decimal ExpenditureKcal, decimal FluxLowKcal, decimal FluxHighKcal, string Status, decimal? ChangeKcal);

/// <summary>Trang Expenditure: chuỗi TDEE mỗi ngày trong khoảng + thống kê + bucket.</summary>
/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày đầu tiên của chuỗi).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Thời gian TĂNG dần, liên tục; rỗng nếu chưa có snapshot nào.</param>
/// <param name="Summary">Thống kê trên ExpenditureKcal các ngày trong khoảng: Average, Difference = ngày cuối − ngày đầu.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record ExpenditureSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ExpenditureSeriesDay> Days,
    MetricSummary Summary,
    IReadOnlyList<MetricBucket> Buckets
);

/// <param name="Days">3 | 7 | 14 | 30 | 90.</param>
/// <param name="ChangeKcal">TDEE hôm nay trừ TDEE N ngày trước; null khi Waiting.</param>
/// <param name="Status">Decrease | Increase | NoChange (thay đổi làm tròn = 0) | Waiting (mốc trước ngày đầu của chuỗi).</param>
public sealed record ExpenditureInsightChange(int Days, decimal? ChangeKcal, string Status);

/// <summary>Insights &amp; Data của trang Expenditure, tính đến hôm nay theo múi giờ user.</summary>
/// <param name="CurrentKcal">TDEE hôm nay; null nếu chưa có snapshot.</param>
/// <param name="Strategy">Calibrating | Updating | Holding: trạng thái hôm nay (null nếu chưa có snapshot).</param>
/// <param name="HoldingReason">Lý do Holding dạng câu (null nếu không Holding hoặc không có lý do cụ thể).</param>
/// <param name="HoldingReasonCodes">MissingLogs (&gt; 3/7 ngày gần nhất không có log hợp lệ) | NoRecentWeighIn (không cân lần nào trong 7 ngày); rỗng nếu không Holding.</param>
/// <param name="CalibratingDaysLeft">Số ngày còn lại của giai đoạn Calibrating; null khi không Calibrating.</param>
/// <param name="LoggedDaysLast7">Số ngày trong 7 ngày gần nhất có log hợp lệ (có entry hoặc nhịn ăn, không đánh dấu chưa đủ).</param>
/// <param name="WeighInDaysLast7">Số ngày trong 7 ngày gần nhất có cân.</param>
public sealed record ExpenditureInsightsResponse(
    DateOnly Today,
    DateOnly? FirstDate,
    decimal? CurrentKcal,
    string? Strategy,
    string? HoldingReason,
    IReadOnlyList<string> HoldingReasonCodes,
    int? CalibratingDaysLeft,
    int LoggedDaysLast7,
    int WeighInDaysLast7,
    IReadOnlyList<ExpenditureInsightChange> Changes
);
