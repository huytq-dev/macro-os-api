namespace Application;

/// <summary>
/// Chuỗi expenditure MỖI NGÀY từ snapshot đầu tiên đến hôm nay + Insights. Hàm thuần. Snapshot chỉ được ghi khi có ghi dữ liệu (log/cân),
/// nên sau lần ghi cuối chuỗi lưu bị đứt; ở đây các ngày thiếu giữ TDEE gần nhất (đúng với thuật toán: không có dữ liệu mới thì TDEE giữ nguyên),
/// Status là Holding (chưa tính được cập nhật); nhãn "đang hiệu chỉnh" là cờ IsCalibrating riêng, không phải trạng thái thuật toán.
/// </summary>
public static class ExpenditureSeries
{
    public static readonly int[] ChangeWindows = [3, 7, 14, 30, 90];

    public static List<ExpenditureSeriesPoint> Daily(IEnumerable<ExpenditureSnapshotRow> snapshots, DateOnly today, DateOnly? start, int calibratingDays, bool freezeDuringCalibration = false, bool smoothLabels = false)
    {
        var byDate = snapshots.Where(s => s.Date <= today).GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.Last());
        var result = new List<ExpenditureSeriesPoint>();
        if (byDate.Count == 0) return result;

        var first = byDate.Keys.Min();
        var prev = byDate[first];
        for (var d = first; d <= today; d = d.AddDays(1))
        {
            var calibrating = start is { } st && ExpenditureCalculator.IsCalibrating(d, st, calibratingDays);
            if (byDate.TryGetValue(d, out var s)) { prev = s; result.Add(new ExpenditureSeriesPoint(d, s.TdeeKcal, s.Status, s.WindowDays, false, calibrating)); }
            else result.Add(new ExpenditureSeriesPoint(d, prev.TdeeKcal, GapDayStatus(d, today, prev.Status, calibrating && freezeDuringCalibration), prev.WindowDays, true, calibrating)); // ngày chưa có dữ liệu mới: giữ TDEE = Holding (nhãn Calibrating tách riêng; trước #3 vẫn là status Calibrating khi còn đóng băng)
        }
        return smoothLabels ? SmoothLabels(result) : result;   // TẮT mặc định: chờ user duyệt trực tiếp (mâu thuẫn "không ân hạn" G0)
    }

    /// <summary>
    /// [CHƯA ĐƯỢC DUYỆT TRỰC TIẾP, TẮT mặc định, không caller production] Hysteresis NHÃN hiển thị (không đổi TDEE, không đổi snapshot lưu): nhãn chỉ đổi khi status mới giữ 2 ngày liên tiếp, để log theo cụm quanh ngưỡng 4/7 không làm nhãn nháy Updating↔Holding.
    /// Rời Calibrating đổi ngay (không trễ lần Updating đầu tiên).
    /// </summary>
    public static List<ExpenditureSeriesPoint> SmoothLabels(List<ExpenditureSeriesPoint> points)
    {
        if (points.Count == 0) return points;
        var shown = points[0].Status;
        var smoothed = new List<ExpenditureSeriesPoint>(points.Count) { points[0] };
        for (var i = 1; i < points.Count; i++)
        {
            var raw = points[i].Status;
            if (raw != shown && (shown == nameof(ExpenditureStatus.Calibrating) || raw == points[i - 1].Status)) shown = raw;
            smoothed.Add(points[i] with { Status = shown });
        }
        return smoothed;
    }

    /// <summary>
    /// Status của ngày chưa có snapshot: HÔM NAY chưa có dữ liệu (chưa log/cân) thì giữ status hôm qua nếu đang Updating (chưa tính là thiếu, không lật Holding chỉ vì qua nửa đêm; cùng quy tắc ExpenditureStatusMachine);
    /// ngày trong quá khứ không có snapshot = không có dữ liệu mới ⇒ Holding (hoặc Calibrating khi còn đóng băng).
    /// </summary>
    private static string GapDayStatus(DateOnly day, DateOnly today, string previousStatus, bool frozenCalibrating)
        => day == today && previousStatus == nameof(ExpenditureStatus.Updating) ? previousStatus
         : frozenCalibrating ? nameof(ExpenditureStatus.Calibrating) : nameof(ExpenditureStatus.Holding);

    /// <summary>
    /// Làm tròn kcal và thay đổi so với hôm trước (từ giá trị chưa làm tròn). Flux Range: dải MACROOS_FLUX_V1 từ <see cref="ExpenditureFluxService"/> khi caller truyền <c>bands</c> (user yêu cầu hiển thị Flux 2026-10-26, thay
    /// quyết định "giữ ẩn" trước đó); không truyền = null. Dải cũ (± hằng số) đã gỡ từ trước và không quay lại.
    /// </summary>
    /// <param name="bands">Dải Flux theo ngày từ <see cref="ExpenditureFluxService"/>; null = không dải (Energy Balance không cần dải).</param>
    public static List<ExpenditureSeriesDay> ToDays(IReadOnlyList<ExpenditureSeriesPoint> raw, IReadOnlyDictionary<DateOnly, FluxPoint>? bands = null)
    {
        var days = new List<ExpenditureSeriesDay>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var band = bands is not null && bands.TryGetValue(raw[i].Date, out var b) ? b : null;
            days.Add(new ExpenditureSeriesDay(raw[i].Date, Round(raw[i].Tdee), band?.LowerKcal, band?.UpperKcal, raw[i].Status, i == 0 ? null : Round(raw[i].Tdee - raw[i - 1].Tdee), raw[i].IsCalibrating));
        }
        return days;
    }

    /// <param name="raw">Chuỗi từ <see cref="Daily"/> kết thúc ở hôm nay.</param>
    /// <param name="last7">Các ngày gần nhất liên tiếp tới hôm nay (khuyến nghị 8 ngày để đủ cửa sổ log khi hôm nay chưa xong): KcalIn có = ngày log hợp lệ, TrendKg có = ngày cân (như ExpenditureRebuilder).</param>
    /// <param name="window">Cửa sổ tính (W ngày kết thúc hôm nay, từ ngày bắt đầu) để chẩn đoán lý do thiếu độ đổi cân nặng; null = chỉ chẩn đoán theo quy tắc 4/7 log + 1/7 cân (hành vi cũ, cho test thuần).</param>
    public static ExpenditureInsightsResponse Insights(IReadOnlyList<ExpenditureSeriesPoint> raw, DateOnly today, DateOnly? start, int calibratingDays, IReadOnlyList<ExpenditureDay> last7, bool freezeDuringCalibration = false,
        IReadOnlyList<ExpenditureDay>? window = null, int windowDays = 21, FluxPoint? flux = null)
    {
        var week = last7.TakeLast(ExpenditureCalculator.RuleDays).ToList();
        var logged = week.Count(d => d.KcalIn.HasValue);
        var weighed = week.Count(d => d.TrendKg.HasValue);
        if (raw.Count == 0)
            return new ExpenditureInsightsResponse(today, null, null, null, null, [], null, logged, weighed, ChangeWindows.Select(n => new ExpenditureInsightChange(n, null, "Waiting")).ToList(),
                start is { } s0 && ExpenditureCalculator.IsCalibrating(today, s0, calibratingDays), ExpenditureCalculator.MinLoggedDaysPer7, ExpenditureCalculator.MinWeighInDaysPer7);

        var now = raw[^1];
        var byDate = raw.ToDictionary(p => p.Date, p => p.Tdee);
        var changes = ChangeWindows.Select(n =>
        {
            if (!byDate.TryGetValue(now.Date.AddDays(-n), out var past)) return new ExpenditureInsightChange(n, null, "Waiting");
            var change = Round(now.Tdee - past);
            return new ExpenditureInsightChange(n, change, change == 0 ? "NoChange" : change < 0 ? "Decrease" : "Increase");
        }).ToList();

        var holding = now.Status == nameof(ExpenditureStatus.Holding);
        var notUpdating = now.Status != nameof(ExpenditureStatus.Updating);
        var metrics = ExpenditureStatusMachine.Metrics(last7, today);
        // Học từ ngày 1 (#3): không đếm ngược "còn N ngày" (CalibratingDaysLeft null, giữ field để tương thích). Trước #3 (đóng băng) vẫn có đếm ngược như cũ.
        int? left = freezeDuringCalibration && now.Status == nameof(ExpenditureStatus.Calibrating) && start is { } s ? Math.Max(0, calibratingDays - (today.DayNumber - s.DayNumber)) : null;
        // Lý do chưa Updating cho Holding VÀ Calibrating, gồm cả lý do thiếu độ đổi cân nặng (cần cửa sổ tính); không có cửa sổ = hành vi cũ (chỉ Holding, chỉ quy tắc 4/7 + 1/7)
        var diag = !notUpdating ? null
            : window is not null ? ExpenditureDiagnostics.Diagnose(last7, window, today, windowDays, left)
            : holding ? new ExpenditureDiagnosis(metrics, ExpenditureStatusMachine.HoldingReasonCodes(metrics), ExpenditureStatusMachine.HoldingReason(metrics), ExpenditureStatusMachine.ResumeInDays(metrics)) : null;
        return new ExpenditureInsightsResponse(today, raw[0].Date, Round(now.Tdee), now.Status, diag?.Reason, diag?.Codes ?? [], left, logged, weighed, changes, now.IsCalibrating, ExpenditureCalculator.MinLoggedDaysPer7, ExpenditureCalculator.MinWeighInDaysPer7,
            ExpenditureLabels.AlgorithmName, diag?.ResumeInDays, ExpenditureStatusMachine.ResumeConsecutiveDays, flux?.LowerKcal, flux?.UpperKcal, flux?.LowerKcal is null ? null : FluxCalculationPolicy.V1Version);
    }

    private static decimal Round(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);
}
