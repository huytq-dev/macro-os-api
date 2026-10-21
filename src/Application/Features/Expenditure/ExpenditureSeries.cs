namespace Application;

/// <summary>
/// Chuỗi expenditure MỖI NGÀY từ snapshot đầu tiên đến hôm nay + Insights. Hàm thuần. Snapshot chỉ được ghi khi có ghi dữ liệu (log/cân),
/// nên sau lần ghi cuối chuỗi lưu bị đứt; ở đây các ngày thiếu giữ TDEE gần nhất (đúng với thuật toán: không có dữ liệu mới thì TDEE giữ nguyên),
/// Status là Calibrating nếu còn trong giai đoạn học, ngược lại Holding (chưa tính được cập nhật).
/// </summary>
public static class ExpenditureSeries
{
    public static readonly int[] ChangeWindows = [3, 7, 14, 30, 90];

    public static List<ExpenditureSeriesPoint> Daily(IEnumerable<ExpenditureSnapshotRow> snapshots, DateOnly today, DateOnly? start, int calibratingDays)
    {
        var byDate = snapshots.Where(s => s.Date <= today).GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.Last());
        var result = new List<ExpenditureSeriesPoint>();
        if (byDate.Count == 0) return result;

        var first = byDate.Keys.Min();
        var prev = byDate[first];
        for (var d = first; d <= today; d = d.AddDays(1))
        {
            if (byDate.TryGetValue(d, out var s)) { prev = s; result.Add(new ExpenditureSeriesPoint(d, s.TdeeKcal, s.Status, s.WindowDays, false)); }
            else
            {
                var calibrating = start is { } st && d.DayNumber - st.DayNumber < calibratingDays;
                result.Add(new ExpenditureSeriesPoint(d, prev.TdeeKcal, calibrating ? nameof(ExpenditureStatus.Calibrating) : nameof(ExpenditureStatus.Holding), prev.WindowDays, true));
            }
        }
        return result;
    }

    /// <summary>Làm tròn kcal, Flux Range từ cửa sổ dữ liệu và thay đổi so với hôm trước (từ giá trị chưa làm tròn).</summary>
    public static List<ExpenditureSeriesDay> ToDays(IReadOnlyList<ExpenditureSeriesPoint> raw, int kcalPerKg, decimal fluxTrendErrorKg)
    {
        var days = new List<ExpenditureSeriesDay>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var half = ExpenditureCalculator.FluxHalfWidth(raw[i].WindowDays, kcalPerKg, fluxTrendErrorKg);
            var tdee = Round(raw[i].Tdee);
            days.Add(new ExpenditureSeriesDay(raw[i].Date, tdee, tdee - half, tdee + half, raw[i].Status, i == 0 ? null : Round(raw[i].Tdee - raw[i - 1].Tdee)));
        }
        return days;
    }

    /// <param name="raw">Chuỗi từ <see cref="Daily"/> kết thúc ở hôm nay.</param>
    /// <param name="last7">7 ngày gần nhất tới hôm nay: KcalIn có = ngày log hợp lệ, TrendKg có = ngày cân (như ExpenditureRebuilder).</param>
    public static ExpenditureInsightsResponse Insights(IReadOnlyList<ExpenditureSeriesPoint> raw, DateOnly today, DateOnly? start, int calibratingDays, IReadOnlyList<ExpenditureDay> last7)
    {
        var logged = last7.Count(d => d.KcalIn.HasValue);
        var weighed = last7.Count(d => d.TrendKg.HasValue);
        if (raw.Count == 0)
            return new ExpenditureInsightsResponse(today, null, null, null, null, [], null, logged, weighed, ChangeWindows.Select(n => new ExpenditureInsightChange(n, null, "Waiting")).ToList());

        var now = raw[^1];
        var byDate = raw.ToDictionary(p => p.Date, p => p.Tdee);
        var changes = ChangeWindows.Select(n =>
        {
            if (!byDate.TryGetValue(now.Date.AddDays(-n), out var past)) return new ExpenditureInsightChange(n, null, "Waiting");
            var change = Round(now.Tdee - past);
            return new ExpenditureInsightChange(n, change, change == 0 ? "NoChange" : change < 0 ? "Decrease" : "Increase");
        }).ToList();

        var holding = now.Status == nameof(ExpenditureStatus.Holding);
        var missing = last7.Count(d => !d.KcalIn.HasValue) + Math.Max(0, 7 - last7.Count);
        var codes = new List<string>();
        if (holding && missing > 3) codes.Add("MissingLogs");
        if (holding && weighed == 0) codes.Add("NoRecentWeighIn");
        int? left = now.Status == nameof(ExpenditureStatus.Calibrating) && start is { } s ? Math.Max(0, calibratingDays - (today.DayNumber - s.DayNumber)) : null;

        return new ExpenditureInsightsResponse(today, raw[0].Date, Round(now.Tdee), now.Status, holding ? ExpenditureCalculator.HoldingReason(last7) : null, codes, left, logged, weighed, changes);
    }

    private static decimal Round(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);
}
