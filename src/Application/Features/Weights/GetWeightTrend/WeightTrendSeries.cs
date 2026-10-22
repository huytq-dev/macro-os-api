namespace Application;

/// <summary>
/// Trend cân nặng MỖI NGÀY (không chỉ ngày có cân) + Insights của trang Weight Trend. Hàm thuần, nguồn dữ liệu duy nhất là các lần cân (WeightEntry).
/// Cùng thuật toán với <see cref="WeightTrendCalculator.Compute"/> (ALG-02): EMA với α = 2 / (N + 1), ngày giữa hai lần cân dùng giá trị NỘI SUY TUYẾN TÍNH
/// (MacroFactor: "missing days are filled by linear interpolation", trung bình nghiêng về gần đây; help.macrofactorapp.com/dashboard/weight_trend).
/// Khác ở chỗ: trả trend cho TỪNG ngày từ lần cân đầu tiên đến ngày kết thúc (hôm nay). Sau lần cân cuối, EMA vẫn chạy với giá trị cân cuối
/// (cùng quy tắc nội suy "giữ giá trị gần nhất": trend tiến dần về số cân cuối nếu không cân thêm).
/// </summary>
public static class WeightTrendSeries
{
    /// <param name="entries">Mỗi ngày tối đa một lần cân (trùng ngày lấy dòng cuối); ngày sau <paramref name="endDate"/> bị bỏ; không cần sắp xếp.</param>
    /// <param name="trendDays">N của EMA (α = 2 / (N + 1)).</param>
    /// <returns>Mỗi ngày liên tục từ ngày cân đầu tiên đến <paramref name="endDate"/>; rỗng nếu chưa có cân.</returns>
    public static List<TrendPoint> Daily(IEnumerable<(DateOnly Date, decimal Kg)> entries, int trendDays, DateOnly endDate)
    {
        var points = entries.Where(e => e.Date <= endDate).GroupBy(e => e.Date).Select(g => g.Last()).OrderBy(e => e.Date).ToList();
        var result = new List<TrendPoint>();
        if (points.Count == 0) return result;

        var alpha = 2m / (trendDays + 1);
        var next = 0;           // chỉ số điểm cân đầu tiên có ngày >= d
        decimal trend = 0;
        for (var d = points[0].Date; d <= endDate; d = d.AddDays(1))
        {
            while (next < points.Count && points[next].Date < d) next++;

            decimal target;
            decimal? scale = null;
            if (next < points.Count && points[next].Date == d) { target = points[next].Kg; scale = target; }
            else if (next < points.Count)
            {
                var prev = points[next - 1];
                var nxt = points[next];
                target = prev.Kg + (nxt.Kg - prev.Kg) * (d.DayNumber - prev.Date.DayNumber) / (nxt.Date.DayNumber - prev.Date.DayNumber);
            }
            else target = points[^1].Kg; // sau lần cân cuối: giữ cân cuối

            trend = result.Count == 0 ? target : alpha * target + (1 - alpha) * trend;
            result.Add(new TrendPoint(d, trend, scale));
        }
        return result;
    }

    /// <summary>Làm tròn 2 chữ số và tính thay đổi so với hôm trước (từ giá trị chưa làm tròn).</summary>
    public static List<WeightTrendDay> ToDays(IReadOnlyList<TrendPoint> raw)
    {
        var days = new List<WeightTrendDay>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
            days.Add(new WeightTrendDay(raw[i].Date, Round(raw[i].Trend), raw[i].Scale is { } s ? Round(s) : null,
                i == 0 ? null : Round(raw[i].Trend - raw[i - 1].Trend)));
        return days;
    }

    public static readonly int[] ChangeWindows = [3, 7, 14, 30, 90];
    /// <summary>Cửa sổ 20 ngày (MacroFactor: weight change rate tính trên 20 ngày) của trend HẰNG NGÀY, không phải số cân thô; phương pháp hồi quy bình phương nhỏ nhất bên dưới là của MacroOS, không phải công thức của MacroFactor.</summary>
    public const int RegressionDays = 20;
    public const int MinRegressionPoints = 7;
    public const decimal NoChangeThresholdKg = 0.05m;

    /// <param name="raw">Chuỗi từ <see cref="Daily"/> kết thúc ở <paramref name="today"/>.</param>
    public static WeightTrendInsightsResponse Insights(IReadOnlyList<TrendPoint> raw, DateOnly today, int trendDays, int kcalPerKg)
    {
        if (raw.Count == 0)
            return new WeightTrendInsightsResponse(today, null, 0, null, ChangeWindows.Select(n => new WeightChangeModel(n, null, "Waiting")).ToList(), null, null, null, trendDays, kcalPerKg,
                InsufficientWeightData, 0, 0, null, 0, MinWeighIns, RegressionDays, RecencyDays, MinSpanDays);

        var current = raw[^1].Trend;
        var byDate = raw.ToDictionary(p => p.Date, p => p.Trend);

        // Thay đổi trend hôm nay so với N ngày trước; chưa có dữ liệu từ N ngày trước thì Waiting
        var changes = ChangeWindows.Select(n =>
        {
            if (!byDate.TryGetValue(raw[^1].Date.AddDays(-n), out var past)) return new WeightChangeModel(n, null, "Waiting");
            var change = Round(current - past);
            return new WeightChangeModel(n, change, Math.Abs(change) < NoChangeThresholdKg ? "NoChange" : change < 0 ? "Decrease" : "Increase");
        }).ToList();

        // Độ dốc hồi quy tuyến tính (bình phương nhỏ nhất, phương pháp của MacroOS) của trend HẰNG NGÀY trong 20 ngày gần nhất, kg/tuần.
        // Chỉ tính khi đủ lần cân THẬT (xem RateGate; ngày nội suy/carry-forward không bao giờ được tính là lần cân) và chuỗi có ≥ 7 ngày; không đủ ⇒ null + mã lý do, KHÔNG bao giờ 0 kg/tuần hoặc 0 kcal
        var window = raw.Where(p => p.Date >= raw[^1].Date.AddDays(-(RegressionDays - 1))).ToList();
        var gate = RateGate(raw.Where(p => p.Scale.HasValue).Select(p => p.Date), raw[^1].Date);
        var reason = gate.Reason ?? (window.Count < MinRegressionPoints ? InsufficientWeightData : null);
        decimal? weekly = null, deficit = null, projected = null;
        if (reason is null)
        {
            var x0 = window[0].Date.DayNumber;
            decimal n = window.Count, sx = 0, sy = 0, sxy = 0, sxx = 0;
            foreach (var p in window)
            {
                var x = (decimal)(p.Date.DayNumber - x0);
                sx += x; sy += p.Trend; sxy += x * p.Trend; sxx += x * x;
            }
            var slopePerDay = (n * sxy - sx * sy) / (n * sxx - sx * sx);
            var perWeek = slopePerDay * 7;
            weekly = Round(perWeek);
            deficit = Math.Round(slopePerDay * kcalPerKg, 0, MidpointRounding.AwayFromZero);   // = tuần × kcal/kg ÷ 7
            projected = Round(current + perWeek * 30 / 7);
        }

        return new WeightTrendInsightsResponse(today, raw[0].Date, raw.Count, Round(current), changes, weekly, deficit, projected, trendDays, kcalPerKg,
            reason, gate.Count, gate.InLast7Days, gate.DaysSinceLast, gate.SpanDays, MinWeighIns, RegressionDays, RecencyDays, MinSpanDays, gate.MaxGapDays, MaxGapDays);
    }

    /// <summary>Tối thiểu để HIỂN THỊ Weekly Change / Energy Deficit / Projection (MACROOS POLICY, MacroFactor không công bố ngưỡng): ≥ 3 ngày có lần cân thật trong 20 ngày VÀ ≥ 1 lần cân trong 7 ngày gần nhất. KHÁC điều kiện cập nhật expenditure (1/7 cân + 4/7 log, máy trạng thái G0): hai thứ độc lập.</summary>
    public const int MinWeighIns = 3, RecencyDays = 7;
    /// <summary>Khoảng cách tối thiểu (ngày) giữa lần cân thật đầu và cuối TRONG cửa sổ (MACROOS POLICY, đã duyệt): ba lần cân dồn trong vài ngày không đủ để hiển thị tốc độ. Qua cổng ≠ hoàn toàn đáng tin (nội suy qua khoảng trống dài vẫn có thể làm lệch).</summary>
    public const int MinSpanDays = 7;
    /// <summary>Khoảng trống tối đa (ngày) giữa hai lần cân thật liên tiếp còn ảnh hưởng cửa sổ 20 ngày (MACROOS POLICY, đã duyệt; KHÔNG khẳng định là ngưỡng của MacroFactor): lớn hơn ⇒ INSUFFICIENT_WEIGHT_DATA. KHÔNG áp dụng cho điều kiện logging/cập nhật của Adaptive Expenditure (G0).</summary>
    public const int MaxGapDays = 14;
    public const string InsufficientWeightData = "INSUFFICIENT_WEIGHT_DATA", StaleWeightData = "STALE_WEIGHT_DATA";

    /// <summary>
    /// Cổng dữ liệu của tốc độ thay đổi (hàm thuần, theo ngày LOCAL của user): đếm NGÀY CÂN THẬT khác nhau trong cửa sổ [today − 19, today] và trong 7 ngày gần nhất [today − 6, today].
    /// Reason: INSUFFICIENT_WEIGHT_DATA khi &lt; 3 ngày cân trong 20 ngày; STALE_WEIGHT_DATA khi đủ 3 nhưng không có lần cân nào trong 7 ngày gần nhất; null = đủ.
    /// SpanDays = số ngày giữa lần cân thật đầu và cuối trong cửa sổ; phải ≥ <see cref="MinSpanDays"/> (3 lần cân dồn trong vài ngày ⇒ INSUFFICIENT_WEIGHT_DATA).
    /// </summary>
    public static (string? Reason, int Count, int InLast7Days, int? DaysSinceLast, int SpanDays, int MaxGapDays) RateGate(IEnumerable<DateOnly> weighInDates, DateOnly today)
    {
        var all = weighInDates.Where(d => d <= today).Distinct().OrderBy(d => d).ToList();
        var windowStart = today.AddDays(-(RegressionDays - 1));
        var inWindow = all.Where(d => d >= windowStart).ToList();
        var last7 = inWindow.Count(d => d >= today.AddDays(-(RecencyDays - 1)));
        int? sinceLast = inWindow.Count == 0 ? null : today.DayNumber - inWindow[^1].DayNumber;
        var span = inWindow.Count < 2 ? 0 : inWindow[^1].DayNumber - inWindow[0].DayNumber;

        // Khoảng trống dài còn ảnh hưởng cửa sổ: mọi cặp lần cân liên tiếp (a, b) mà b nằm TRONG cửa sổ (kể cả khi a nằm trước cửa sổ: khoảng trống cắt ngang biên) — các ngày nội suy của khoảng đó
        // và trend EMA thừa hưởng từ trước khoảng trống đều nằm trong hồi quy. Khoảng trống đã kết thúc TRƯỚC cửa sổ không còn tính ⇒ tự khôi phục khi dữ liệu mới đủ (không Waiting vô thời hạn).
        var maxGap = 0;
        for (var i = 1; i < all.Count; i++)
            if (all[i] >= windowStart) maxGap = Math.Max(maxGap, all[i].DayNumber - all[i - 1].DayNumber);

        // Thứ tự: thiếu số ngày cân, dồn quá gần nhau (span < MinSpanDays) hoặc có khoảng trống > MaxGapDays ⇒ INSUFFICIENT; đủ nhưng không có lần cân nào trong 7 ngày gần nhất ⇒ STALE
        var reason = inWindow.Count < MinWeighIns || span < MinSpanDays || maxGap > MaxGapDays ? InsufficientWeightData : last7 == 0 ? StaleWeightData : null;
        return (reason, inWindow.Count, last7, sinceLast, span, maxGap);
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
