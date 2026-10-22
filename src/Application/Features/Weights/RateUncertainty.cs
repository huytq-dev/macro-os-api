namespace Application;

/// <summary>
/// Ước lượng tốc độ thay đổi cân (kg/tuần) từ hồi quy 20 ngày của trend EMA hằng ngày (cùng đường tính với <see cref="WeightTrendSeries.Insights"/>) KÈM sai số chuẩn và sai lệch khởi động. Hàm thuần, KHÔNG nối vào luồng nào (INACTIVE).
/// <para>
/// Sai số chuẩn: các điểm trend EMA/nội suy KHÔNG phải quan sát độc lập. Độ dốc là tổ hợp TUYẾN TÍNH của các lần cân thật: <c>slope = Σ cᵢ·yᵢ</c> (cᵢ tính chính xác bằng đáp ứng của cùng đường tính với vector đơn vị),
/// nên <c>Var(slope) = σ² · cᵀRc</c> với R là ma trận tương quan của nhiễu giữa các lần cân (AR(1): ρ^|ngày cách|, ρ ước lượng từ phần dư, co về ρ₀ = 0.5 với trọng số 8 cặp) — tính cả tự tương quan do nước/glycogen.
/// σ, ρ lấy từ phần dư của đường thẳng OLS trên 28 ngày gần nhất (cần ≥ 6 lần cân; không đủ ⇒ không có SE).
/// </para>
/// <para>
/// Sai lệch khởi động (warm-up) φ: trend EMA khởi tạo ở lần cân đầu nên độ dốc bị thấp hơn thật theo hệ số (1 + φ) trong vài tuần đầu; φ chỉ phụ thuộc NGÀY cân (đáp ứng của đường tính với đường thẳng 1 kg/tuần không nhiễu), φ ≈ −0.33 ở tuổi 21 ngày, −0.08 ở 35, −0.01 ở 56.
/// </para>
/// Hiệu chỉnh/kiểm chứng bằng mô phỏng tổng hợp: Docs/Design/Verification/CheckIn_OnTarget_Calibration_Report.md.
/// </summary>
public static class RateUncertainty
{
    public const int NoiseWindowDays = 28, MinNoisePoints = 6;
    public const double RhoPrior = 0.5, RhoPriorWeight = 8;

    /// <param name="WarmupBias">φ: độ dốc ước lượng ≈ (1 + φ) × độ dốc thật khi chuỗi còn trẻ (φ ≤ 0).</param>
    public sealed record RateEstimate(double RateKgPerWeek, double? StandardError, double WarmupBias, double? Sigma, double? Rho, int NoisePoints);

    /// <param name="entries">Lần cân (mỗi ngày tối đa một, trùng ngày lấy dòng cuối); ngày sau <paramref name="today"/> bị bỏ.</param>
    /// <returns>null nếu &lt; 3 lần cân.</returns>
    public static RateEstimate? Estimate(IEnumerable<(DateOnly Date, decimal Kg)> entries, DateOnly today, int trendDays)
    {
        var points = entries.Where(e => e.Date <= today).GroupBy(e => e.Date).Select(g => g.Last()).OrderBy(e => e.Date).ToList();
        if (points.Count < 3) return null;
        var ds = points.Select(p => p.Date.DayNumber).ToArray();
        var y = points.Select(p => (double)p.Kg).ToArray();
        var t = today.DayNumber;
        var alpha = 2.0 / (trendDays + 1);

        var (slope, c) = SlopeAndWeights(ds, y, t, alpha);
        var line = ds.Select(d => 80.0 + (d - ds[0]) / 7.0).ToArray();
        var phi = SlopeAndWeights(ds, line, t, alpha).Slope - 1.0;

        var noise = NoiseStats(ds, y, t);
        if (noise is null) return new RateEstimate(slope, null, phi, null, null, 0);
        var (sigma, rho, n) = noise.Value;

        // Chỉ các lần cân có trọng số đáng kể (trend EMA quên dần quá khứ) tham gia: O(k²) thay vì O(n²)
        var maxC = c.Max(Math.Abs);
        var idx = Enumerable.Range(0, ds.Length).Where(i => Math.Abs(c[i]) > 1e-12 * Math.Max(maxC, 1e-300)).ToArray();
        double variance = 0;
        foreach (var a in idx)
            foreach (var b in idx)
                variance += c[a] * c[b] * Math.Pow(rho, Math.Abs(ds[a] - ds[b]));
        return new RateEstimate(slope, sigma * Math.Sqrt(Math.Max(variance, 0)), phi, sigma, rho, n);
    }

    /// <summary>Độ dốc (kg/tuần) của hồi quy 20 ngày trên trend EMA hằng ngày và trọng số tuyến tính cᵢ (slope = Σ cᵢ·yᵢ).</summary>
    public static (double Slope, double[] C) SlopeAndWeights(int[] ds, double[] y, int today, double alpha)
    {
        var n = ds.Length;
        var winStart = Math.Max(ds[0], today - WeightTrendSeries.RegressionDays + 1);
        var winLen = today - winStart + 1;
        var mean = winStart + (winLen - 1) / 2.0;
        double sxx = 0;
        for (var d = winStart; d <= today; d++) sxx += (d - mean) * (d - mean);
        double Weight(int d) => 7 * (d - mean) / sxx;

        // trend EMA theo ngày cho y (nội suy tuyến tính, giữ giá trị cuối sau lần cân cuối, khởi tạo ở lần cân đầu)
        double slope = 0, tr = 0; var j = 0; var first = true;
        for (var d = ds[0]; d <= today; d++)
        {
            while (j < n && ds[j] < d) j++;
            double tgt = j < n && ds[j] == d ? y[j] : j < n ? y[j - 1] + (y[j] - y[j - 1]) * (d - ds[j - 1]) / (ds[j] - ds[j - 1]) : y[n - 1];
            tr = first ? tgt : alpha * tgt + (1 - alpha) * tr; first = false;
            if (d >= winStart) slope += Weight(d) * tr;
        }

        var c = new double[n];
        for (var i = 0; i < n; i++)
        {
            var d0 = i == 0 ? ds[0] : ds[i - 1];
            var jj = i == 0 ? 0 : i - 1;
            double t = 0, ci = 0; var started = false;
            for (var d = d0; d <= today; d++)
            {
                while (jj < n && ds[jj] < d) jj++;
                double target;
                if (jj < n && ds[jj] == d) target = jj == i ? 1 : 0;
                else if (jj < n)
                {
                    double ya = jj - 1 == i ? 1 : 0, yb = jj == i ? 1 : 0;
                    target = ya + (yb - ya) * (d - ds[jj - 1]) / (ds[jj] - ds[jj - 1]);
                }
                else target = i == n - 1 ? 1 : 0;

                if (!started) { t = i == 0 ? target : alpha * target + (1 - alpha) * 0.0; started = true; }
                else t = alpha * target + (1 - alpha) * t;
                if (d >= winStart) ci += Weight(d) * t;
            }
            c[i] = ci;
        }
        return (slope, c);
    }

    /// <summary>σ và ρ (co về ρ₀) của phần dư đường thẳng OLS trên 28 ngày gần nhất; null nếu &lt; 6 lần cân.</summary>
    public static (double Sigma, double Rho, int Points)? NoiseStats(int[] ds, double[] y, int today)
    {
        var idx = Enumerable.Range(0, ds.Length).Where(i => ds[i] >= today - (NoiseWindowDays - 1)).ToArray();
        if (idx.Length < MinNoisePoints) return null;
        var xs = idx.Select(i => (double)ds[i]).ToArray(); var ys = idx.Select(i => y[i]).ToArray(); var n = xs.Length;
        var mx = xs.Average(); var my = ys.Average();
        var sxx = xs.Sum(x => (x - mx) * (x - mx));
        var b = xs.Zip(ys, (x, v) => (x - mx) * (v - my)).Sum() / sxx;
        var res = xs.Zip(ys, (x, v) => v - (my + b * (x - mx))).ToArray();
        var sigma = Math.Sqrt(res.Sum(e => e * e) / (n - 2));

        double num = 0, den = 0;
        for (var gap = 1; gap <= 3; gap++)
        {
            var pairs = new List<(double A, double B)>();
            for (var a = 0; a < n; a++)
                for (var bb = a + 1; bb < n; bb++)
                    if (xs[bb] - xs[a] == gap) pairs.Add((res[a], res[bb]));
            var sa = pairs.Sum(p => p.A * p.A); var sb = pairs.Sum(p => p.B * p.B);
            if (sa > 0 && sb > 0 && pairs.Count >= 2)
            {
                var cg = pairs.Sum(p => p.A * p.B) / Math.Sqrt(sa * sb);
                var rhoG = Math.Pow(Math.Max(0.0, Math.Min(0.95, cg)), 1.0 / gap);
                num += rhoG * pairs.Count; den += pairs.Count;
            }
        }
        var emp = den > 0 ? num / den : RhoPrior;
        var rho = (den * emp + RhoPriorWeight * RhoPrior) / (den + RhoPriorWeight);
        return (sigma, rho, n);
    }
}
