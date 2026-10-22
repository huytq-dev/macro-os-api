namespace Contract;

/// <summary>Insights &amp; Data của trang Weight Trend, tính đến hôm nay theo múi giờ user, từ các lần cân (Scale Weight).</summary>
/// <param name="FirstWeighInDate">Ngày cân đầu tiên; null nếu chưa có cân.</param>
/// <param name="DataDays">Số ngày của chuỗi trend (từ lần cân đầu tới hôm nay).</param>
/// <param name="CurrentWeightKg">Trend hôm nay: ước lượng cân nặng thật sau khi làm mượt.</param>
/// <param name="WeeklyChangeKg">Độ dốc hồi quy tuyến tính (phương pháp của MacroOS) của trend HẰNG NGÀY trong 20 ngày gần nhất (cửa sổ 20 ngày theo MacroFactor), kg/tuần (âm = giảm); null nếu có &lt; 7 ngày dữ liệu.</param>
/// <param name="EnergyDeficitKcalPerDay">WeeklyChange × KcalPerKg ÷ 7, kcal/ngày, làm tròn; ÂM khi đang giảm cân (như MacroFactor hiện "−192").</param>
/// <param name="RateReasonCode">null = tốc độ/thâm hụt/dự báo đã tính; INSUFFICIENT_WEIGHT_DATA (&lt; <c>MinWeighIns</c> ngày cân thật trong <c>RateWindowDays</c> ngày, HOẶC lần cân thật đầu–cuối trong cửa sổ cách nhau &lt; <c>MinSpanDays</c> ngày, hoặc chuỗi &lt; 7 ngày) | STALE_WEIGHT_DATA (đủ lần cân nhưng không có lần cân nào trong <c>RecencyDays</c> ngày gần nhất). Khi khác null thì WeeklyChangeKg, EnergyDeficitKcalPerDay, Projected30DaysKg đều null (FE hiện "Waiting —", KHÔNG bao giờ 0); trend và biểu đồ vẫn hợp lệ. MACROOS POLICY: ≥ 3/20 ngày và ≥ 1/7 ngày chỉ là điều kiện HIỂN THỊ, không liên quan điều kiện cập nhật expenditure.</param>
/// <param name="WeighInCount">Số NGÀY cân thật khác nhau trong cửa sổ 20 ngày (ngày nội suy/carry-forward không tính), theo ngày local của user.</param>
/// <param name="WeighInsLast7Days">Số ngày cân thật trong 7 ngày gần nhất (gồm hôm nay).</param>
/// <param name="DaysSinceLastWeighIn">Số ngày từ lần cân cuối trong cửa sổ đến hôm nay; null nếu không có.</param>
/// <param name="WeighInSpanDays">Khoảng cách (ngày) giữa lần cân đầu và cuối trong cửa sổ (chỉ báo; chưa chặn).</param>
/// <param name="MaxGapDays">Khoảng trống dài nhất (ngày) giữa hai lần cân thật liên tiếp mà lần sau nằm trong cửa sổ (kể cả khoảng trống bắt đầu trước cửa sổ). &gt; <c>MaxGapDaysLimit</c> (14, MACROOS POLICY) ⇒ RateReasonCode = INSUFFICIENT_WEIGHT_DATA; tự khôi phục khi khoảng trống rời khỏi cửa sổ.</param>
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
    int KcalPerKg,
    string? RateReasonCode = null,
    int WeighInCount = 0,
    int WeighInsLast7Days = 0,
    int? DaysSinceLastWeighIn = null,
    int WeighInSpanDays = 0,
    int MinWeighIns = 3,
    int RateWindowDays = 20,
    int RecencyDays = 7,
    int MinSpanDays = 7,
    int MaxGapDays = 0,
    int MaxGapDaysLimit = 14
);
