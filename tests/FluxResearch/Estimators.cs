using Application;
using Domain;

namespace FluxResearch;

/// <summary>
/// Các bộ ước lượng TDEE so sánh trong nghiên cứu một vòng (KHÔNG thay production). Mọi hàm nhận một <see cref="World"/> và trả TDEE ước lượng SAU dữ liệu của ngày d, d = 0..n−1 (nhân quả: chỉ dùng dữ liệu ≤ d).
/// Nhiễu quan sát giả định ở đây là tham số của bộ lọc, được HIỆU CHỈNH trên tập calibration (không đọc từ mô phỏng).
/// </summary>
public static class Estimators
{
    private const double KcalPerKg = 7700;

    /// <summary>Production: chuỗi TDEE đã học của bản sao engine (cửa sổ 21 ngày trên trend EMA 20 ngày, rồi EMA β 0.1, máy trạng thái G0, đóng băng Calibrating 14 ngày).</summary>
    public static double[] Production(World w) => w.Learned.Select(l => (double)l.TdeeKcal).ToArray();

    /// <summary>CÙNG cấu trúc production (hàm production nguyên bản, cùng cổng Updating/Holding) nhưng (β, W) khác: kiểm tra xem cấu trúc đúng mà chỉnh tham số có đủ không.</summary>
    public static double[] ProductionTuned(World w, decimal beta, int window)
        => ExpenditureFluxCalculator.Trajectory(w.Observations, w.Learned, w.Start, new FluxCandidateSpec("t", beta, window), w.KcalPerKg).Select(x => (double)x).ToArray();

    // ---------------- Kalman / IMM trên trạng thái [cân thật W (kg), TDEE T (kcal/ngày)] ----------------
    // W[d+1] = W[d] + (I[d] − T[d]) / 7700 (I = kcal ăn đã log, đầu vào đã biết, nhiễu σ_I);  T[d+1] = T[d] + w_T (bước ngẫu nhiên σ_T);  quan sát: cân trên cân z = W + v (σ_V, gồm cả nước).
    private struct Model { public double W, T, P00, P01, P11; }

    private static Model Predict(Model m, double intake, double sigI, double sigT)
    {
        const double a = -1.0 / KcalPerKg;
        var qW = sigI / KcalPerKg * (sigI / KcalPerKg);
        return new Model { W = m.W + (intake - m.T) / KcalPerKg, T = m.T, P00 = m.P00 + 2 * a * m.P01 + a * a * m.P11 + qW, P01 = m.P01 + a * m.P11, P11 = m.P11 + sigT * sigT };
    }

    private static (Model Post, double Innovation, double S) Update(Model m, double z, double sigV)
    {
        var s = m.P00 + sigV * sigV; var y = z - m.W; var k0 = m.P00 / s; var k1 = m.P01 / s;
        return (new Model { W = m.W + k0 * y, T = m.T + k1 * y, P00 = (1 - k0) * m.P00, P01 = (1 - k0) * m.P01, P11 = m.P11 - k1 * m.P01 }, y, s);
    }

    /// <summary>Bộ lọc Kalman một mô hình. Ngày thiếu log (null) dùng trung bình ăn gần đây với σ lớn (σ_miss); không bao giờ coi là 0 kcal; ngày nhịn ăn hợp lệ là 0 kcal.</summary>
    public static double[] Kalman(World w, double sigT, double sigV, double sigI, double sigMiss = 400, double sigT0 = 300, double sigW0 = 0.7, double[]? sdOut = null)
        => Imm(w, [sigT], [[1.0]], sigV, sigI, sigMiss, sigT0, sigW0, sdOut);

    /// <summary>IMM: các mô hình cùng cấu trúc, khác σ_T (ổn định ↔ đang đổi), ma trận chuyển trạng thái Markov; ước lượng = trung bình theo xác suất mô hình.</summary>
    public static double[] Imm(World w, double[] sigT, double[][] transition, double sigV, double sigI, double sigMiss = 400, double sigT0 = 300, double sigW0 = 0.7, double[]? sdOut = null)
    {
        var n = w.Learned.Count; var k = sigT.Length;
        var est = new double[n]; var t0 = (double)w.Learned[0].TdeeKcal;
        var cur = t0; var init = false;
        var models = new Model[k]; var mu = Enumerable.Repeat(1.0 / k, k).ToArray();
        var meanIntake = t0; double prevU = 0, prevSig = sigMiss;
        for (var d = 0; d < n; d++)
        {
            if (init)
            {
                // trộn
                var c = new double[k]; var mixed = new Model[k];
                for (var j = 0; j < k; j++) c[j] = Enumerable.Range(0, k).Sum(i => transition[i][j] * mu[i]);
                for (var j = 0; j < k; j++)
                {
                    var wts = Enumerable.Range(0, k).Select(i => transition[i][j] * mu[i] / c[j]).ToArray();
                    var w0 = 0.0; var tt = 0.0; for (var i = 0; i < k; i++) { w0 += wts[i] * models[i].W; tt += wts[i] * models[i].T; }
                    var p00 = 0.0; var p01 = 0.0; var p11 = 0.0;
                    for (var i = 0; i < k; i++) { var dw = models[i].W - w0; var dt = models[i].T - tt; p00 += wts[i] * (models[i].P00 + dw * dw); p01 += wts[i] * (models[i].P01 + dw * dt); p11 += wts[i] * (models[i].P11 + dt * dt); }
                    mixed[j] = new Model { W = w0, T = tt, P00 = p00, P01 = p01, P11 = p11 };
                }
                var logL = new double[k]; var hasObs = w.RawWeight[d] is not null;
                for (var j = 0; j < k; j++)
                {
                    models[j] = Predict(mixed[j], prevU, prevSig, sigT[j]);
                    if (hasObs) { var (post, y, s) = Update(models[j], w.RawWeight[d]!.Value, sigV); models[j] = post; logL[j] = -0.5 * y * y / s - 0.5 * Math.Log(2 * Math.PI * s); }
                }
                if (hasObs)
                {
                    var max = logL.Max(); var raw = Enumerable.Range(0, k).Select(j => Math.Exp(logL[j] - max) * c[j]).ToArray(); var sum = raw.Sum();
                    for (var j = 0; j < k; j++) mu[j] = raw[j] / sum;
                }
                else for (var j = 0; j < k; j++) mu[j] = c[j];
                cur = Enumerable.Range(0, k).Sum(j => mu[j] * models[j].T);
            }
            else if (w.RawWeight[d] is { } first)
            {
                for (var j = 0; j < k; j++) models[j] = new Model { W = first, T = t0, P00 = sigW0 * sigW0, P01 = 0, P11 = sigT0 * sigT0 };
                init = true; cur = t0;
            }
            est[d] = cur;
            if (sdOut is not null) sdOut[d] = init ? Math.Sqrt(Enumerable.Range(0, k).Sum(j => mu[j] * (models[j].P11 + (models[j].T - cur) * (models[j].T - cur)))) : sigT0;   // độ lệch chuẩn hậu nghiệm của TDEE (hỗn hợp)

            var kcal = w.Observations[d].KcalIn;
            if (kcal is { } v) { prevU = (double)v; prevSig = sigI; meanIntake = 0.9 * meanIntake + 0.1 * prevU; }
            else { prevU = meanIntake; prevSig = sigMiss; }
        }
        return est;
    }

    /// <summary>
    /// Độ dốc cân bền vững (Theil–Sen trên các lần cân THÔ, không qua trend EMA) trong cửa sổ W ngày; TDEE_raw = trung bình kcal hợp lệ trong khoảng cân đầu–cuối − dốc × 7700; làm mượt EMA β; thiếu dữ liệu thì giữ.
    /// </summary>
    public static double[] RobustSlope(World w, int window, double beta)
    {
        var n = w.Learned.Count; var est = new double[n]; var cur = (double)w.Learned[0].TdeeKcal;
        for (var d = 0; d < n; d++)
        {
            var pts = Enumerable.Range(Math.Max(0, d - window + 1), Math.Min(window, d + 1)).Where(i => w.RawWeight[i] is not null).Select(i => (Day: i, Kg: w.RawWeight[i]!.Value)).ToList();
            if (pts.Count >= 4 && pts[^1].Day - pts[0].Day >= 7)
            {
                var slopes = new List<double>();
                for (var i = 0; i < pts.Count; i++) for (var j = i + 1; j < pts.Count; j++) slopes.Add((pts[j].Kg - pts[i].Kg) / (pts[j].Day - pts[i].Day));
                var slope = Metrics.Median(slopes);
                var intakes = Enumerable.Range(pts[0].Day, pts[^1].Day - pts[0].Day + 1).Select(i => w.Observations[i].KcalIn).Where(x => x.HasValue).Select(x => (double)x!.Value).ToList();
                if (intakes.Count > 0) cur = beta * (intakes.Average() - slope * KcalPerKg) + (1 - beta) * cur;
            }
            est[d] = cur;
        }
        return est;
    }
}
