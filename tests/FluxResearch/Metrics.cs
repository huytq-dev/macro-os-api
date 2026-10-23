using Application;
using Domain;

namespace FluxResearch;

/// <summary>Một tập dải để so sánh: tên + giới hạn từng ngày (null = không có dải).</summary>
public sealed record BandSet(string Name, double?[] Lower, double?[] Upper);

/// <summary>Chỉ số của MỘT thế giới (kịch bản × seed) với MỘT tập dải. Độ rộng KHÔNG được đánh giá như độ phủ khoảng tin cậy; "TruthInBand" chỉ để chẩn đoán.</summary>
public sealed record WorldMetrics(
    double LearnedMae, double WidthMean, double WidthStable, double WidthChange, double FalseExpansion,
    double? OnsetLagDays, double? RecoveryDays, double Flicker, double TruthInBand, double? LearnedHalfDays);

public static class Metrics
{
    public const int Warmup = 28;
    public const int ChangeWindow = 14;
    public const double StableMax = 50, ChangeMin = 150;

    /// <summary>Độ lớn thay đổi THẬT của expenditure trong 14 ngày gần nhất tại từng ngày (max − min của TDEE thật).</summary>
    public static double[] ChangeMagnitude(double[] truth, int window = ChangeWindow)
        => truth.Select((_, d) => { var w = truth.Skip(Math.Max(0, d - window + 1)).Take(Math.Min(window, d + 1)); return w.Max() - w.Min(); }).ToArray();

    public static double Mean(IEnumerable<double> xs) { var l = xs.ToList(); return l.Count == 0 ? double.NaN : l.Average(); }

    public static double Median(IEnumerable<double> xs)
    {
        var l = xs.OrderBy(x => x).ToList();
        if (l.Count == 0) return double.NaN;
        return l.Count % 2 == 1 ? l[l.Count / 2] : (l[l.Count / 2 - 1] + l[l.Count / 2]) / 2;
    }

    public static double Spearman(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count < 3) return double.NaN;
        var rx = Ranks(x); var ry = Ranks(y);
        var mx = rx.Average(); var my = ry.Average();
        var cov = rx.Zip(ry).Sum(p => (p.First - mx) * (p.Second - my));
        var vx = rx.Sum(a => (a - mx) * (a - mx)); var vy = ry.Sum(a => (a - my) * (a - my));
        return vx == 0 || vy == 0 ? double.NaN : cov / Math.Sqrt(vx * vy);
    }

    private static double[] Ranks(IReadOnlyList<double> v)
    {
        var order = Enumerable.Range(0, v.Count).OrderBy(i => v[i]).ToArray();
        var ranks = new double[v.Count];
        for (var i = 0; i < order.Length;)
        {
            var j = i;
            while (j + 1 < order.Length && v[order[j + 1]] == v[order[i]]) j++;
            var avg = (i + j) / 2.0 + 1;
            for (var k = i; k <= j; k++) ranks[order[k]] = avg;
            i = j + 1;
        }
        return ranks;
    }

    /// <summary>Dải từ FluxPoint (của calculator) sang BandSet.</summary>
    public static BandSet FromPoints(string name, IReadOnlyList<FluxPoint> points)
        => new(name, points.Select(p => p.LowerKcal is { } l ? (double?)l : null).ToArray(), points.Select(p => p.UpperKcal is { } u ? (double?)u : null).ToArray());

    /// <summary>Dải CŨ (±round(0.1×7700 ÷ max(WindowDays,7))) quanh TDEE đã làm tròn, vẽ ở mọi trạng thái như production hiện tại.</summary>
    public static BandSet Legacy(World w)
    {
        var lower = new double?[w.Learned.Count]; var upper = new double?[w.Learned.Count];
        for (var i = 0; i < w.Learned.Count; i++)
        {
            var half = Math.Round(0.1 * w.KcalPerKg / Math.Max(w.WindowCounts[i], 7));   // công thức cũ (đã gỡ khỏi API), giữ lại làm đường cơ sở nghiên cứu
            var tdee = Math.Round((double)w.Learned[i].TdeeKcal, 0, MidpointRounding.AwayFromZero);
            lower[i] = tdee - half; upper[i] = tdee + half;
        }
        return new BandSet("L0 legacy", lower, upper);
    }

    public static WorldMetrics Evaluate(World w, BandSet band, double tau, double[] changeMag)
    {
        var n = w.Learned.Count;
        var days = Enumerable.Range(Warmup, Math.Max(0, n - Warmup)).Where(d => band.Lower[d] is not null).ToList();
        var width = (int d) => band.Upper[d]!.Value - band.Lower[d]!.Value;
        var stable = days.Where(d => changeMag[d] <= StableMax).ToList();
        var change = days.Where(d => changeMag[d] >= ChangeMin).ToList();
        var mae = Mean(Enumerable.Range(Warmup, Math.Max(0, n - Warmup)).Where(d => w.Learned[d].Status != ExpenditureStatus.Calibrating).Select(d => Math.Abs((double)w.Learned[d].TdeeKcal - w.Truth[d])));

        double flicker = Mean(stable.Where(d => d > 0 && band.Lower[d - 1] is not null).Select(d => Math.Abs(band.Lower[d]!.Value - band.Lower[d - 1]!.Value) + Math.Abs(band.Upper[d]!.Value - band.Upper[d - 1]!.Value)));
        double? onset = null, recovery = null, half = null;
        if (w.Scenario.EventDay is { } ev)
        {
            var o = Enumerable.Range(ev, n - ev).FirstOrDefault(d => band.Lower[d] is not null && width(d) > tau, -1);
            if (o >= 0)
            {
                onset = o - ev;
                var r = Enumerable.Range(o, n - o - 7).FirstOrDefault(d => Enumerable.Range(d, 7).All(k => band.Lower[k] is not null && width(k) <= tau), -1);
                recovery = r >= 0 ? r - ev : null;
            }
            var before = w.Truth[Math.Max(0, ev - 1)]; var after = w.Truth[n - 1];
            if (Math.Abs(after - before) >= 100)
            {
                var h = Enumerable.Range(ev, n - ev).FirstOrDefault(d => Math.Abs((double)w.Learned[d].TdeeKcal - after) <= 0.5 * Math.Abs(after - before), -1);
                half = h >= 0 ? h - ev : null;
            }
        }
        return new WorldMetrics(
            mae, Mean(days.Select(d => width(d))), Mean(stable.Select(d => width(d))), Mean(change.Select(d => width(d))),
            stable.Count == 0 ? double.NaN : stable.Count(d => width(d) > tau) / (double)stable.Count,
            onset, recovery, flicker, days.Count == 0 ? double.NaN : days.Count(d => w.Truth[d] >= band.Lower[d]!.Value && w.Truth[d] <= band.Upper[d]!.Value) / (double)days.Count, half);
    }
}
