using System.Globalization;
using Domain;

namespace FluxResearch;

/// <summary>
/// MỘT vòng nghiên cứu bộ ước lượng TDEE (không thay production): production vs production-tinh-chỉnh vs Kalman vs IMM vs độ dốc cân bền vững, trên CÙNG các kịch bản tổng hợp có sự thật nền.
/// Tham số mọi bộ ước lượng được chọn CHỈ trên tập calibration (S01, S02, S04, S06, S09, S10 × seed 1–20) theo MAE; đánh giá trên TẤT CẢ kịch bản với seed 101–120 (kịch bản giữ lại + seed mới).
/// </summary>
public static class EstimatorStudy
{
    private sealed record Cand(string Family, string Label, Func<World, double[]> Run);

    private static readonly string[] Steady = ["S01", "S13", "S14", "S15", "S16", "S17"];
    private static readonly string[] Confound = ["S06", "S07", "S08"];
    private static readonly string[] Gaps = ["S09", "S10"];
    private static readonly string[] Change = ["S02", "S03", "S04", "S05", "S11"];
    private static readonly string[] Start = ["S12a", "S12b"];
    private static readonly HashSet<string> ConstantTruth = [.. Steady, .. Confound, .. Gaps, .. Start];

    private static string F(double v, string fmt = "0.0") => double.IsNaN(v) ? "-" : v.ToString(fmt, CultureInfo.InvariantCulture);
    private static double Mean(IEnumerable<double> xs) { var l = xs.Where(x => !double.IsNaN(x)).ToList(); return l.Count == 0 ? double.NaN : l.Average(); }
    private static string Fn(IEnumerable<double?> xs) { var all = xs.ToList(); var l = all.Where(x => x.HasValue).Select(x => x!.Value).ToList(); return l.Count == 0 ? "-" : $"{l.Average():0.0} ({l.Count}/{all.Count})"; }

    private const int From = Metrics.Warmup;
    private static double Mae(World w, double[] e) => Mean(Enumerable.Range(From, w.Learned.Count - From).Select(d => Math.Abs(e[d] - w.Truth[d])));
    private static double Rmse(World w, double[] e) => Math.Sqrt(Mean(Enumerable.Range(From, w.Learned.Count - From).Select(d => (e[d] - w.Truth[d]) * (e[d] - w.Truth[d]))));
    private static double Forecast(World w, double[] e, int h) => Mean(Enumerable.Range(From, Math.Max(0, w.Learned.Count - From - h)).Select(d => Math.Abs(e[d] - w.Truth[d + h])));
    private static double FalseChange(World w, double[] e, double thr) => Mean(Enumerable.Range(42, w.Learned.Count - 42).Select(d => Math.Abs(e[d] - e[d - 14]) > thr ? 1.0 : 0.0));
    private static double Flicker(World w, double[] e) => Mean(Enumerable.Range(From, w.Learned.Count - From).Select(d => Math.Abs(e[d] - e[d - 1])));

    private static (double? Half, double? Recovery) StepTimes(World w, double[] e)
    {
        var ev = w.Scenario.EventDay!.Value; var n = w.Learned.Count; var before = w.Truth[ev - 1]; var after = w.Truth[n - 1];
        var h = Enumerable.Range(ev, n - ev).FirstOrDefault(d => Math.Abs(e[d] - after) <= 0.5 * Math.Abs(after - before), -1);
        var r = Enumerable.Range(ev, n - ev - 7).FirstOrDefault(d => Enumerable.Range(d, 7).All(k => Math.Abs(e[k] - after) <= 100), -1);
        return (h >= 0 ? h - ev : null, r >= 0 ? r - ev : null);
    }

    /// <summary>
    /// Phần ĐÓNG của nghiên cứu (không tìm tham số mới): đánh giá riêng β ∈ {0.035, 0.05} (+ 0.07 làm điểm trung gian) với cửa sổ 14 ngày so với production, CHỈ trên seed chưa dùng (101–120), tách kịch bản giữ lại ·
    /// kịch bản hiệu chỉnh seed mới. Quy tắc chấp nhận được CHỐT TRƯỚC khi xem số: ứng viên "vượt baseline" khi (1) MAE tổng giảm có ý nghĩa (CI bootstrap không chứa 0), (2) ngày đổi giả giảm,
    /// (3) nửa bước phản ứng ≤ production × 1.25, (4) MAE nhóm "TDEE thật đổi" ≤ production × 1.15 (không ưu tiên MAE thấp nếu phản ứng chậm hơn đáng kể).
    /// </summary>
    public static int RunClosing()
    {
        var valSeeds = Enumerable.Range(101, 20).ToArray();
        var worlds = Scenarios.All.Where(s => s.Id != "S18").SelectMany(s => valSeeds.Select(seed => WorldBuilder.Build(s, seed))).ToList();
        var cands = new (string Name, Func<World, double[]> Run)[]
        {
            ("Production (β 0.1, W 21)", Estimators.Production),
            ("β 0.07, W 14", w => Estimators.ProductionTuned(w, 0.07m, 14)),
            ("β 0.05, W 14", w => Estimators.ProductionTuned(w, 0.05m, 14)),
            ("β 0.035, W 14", w => Estimators.ProductionTuned(w, 0.035m, 14)),
        };
        var est = cands.ToDictionary(c => c.Name, c => worlds.Select(w => c.Run(w)).ToArray());
        var names = cands.Select(c => c.Name).ToArray();
        IEnumerable<int> Idx(IEnumerable<string> ids) => Enumerable.Range(0, worlds.Count).Where(i => ids.Contains(worlds[i].Scenario.Id));
        string Row(string title, IEnumerable<string> ids, Func<int, string, string> cell) => $"| {title} | " + string.Join(" | ", names.Select(nm => cell(0, nm))) + " |";

        Console.WriteLine("# Closing evaluation — production vs β 0.035–0.07 with a 14-day window (fresh seeds 101–120)\n");
        Console.WriteLine("Acceptance rule fixed before looking at the numbers: a candidate beats the baseline only if (1) overall MAE is lower with a bootstrap CI excluding 0, (2) false-change days drop, (3) half-step response ≤ 1.25 × production, (4) MAE when the true TDEE changes ≤ 1.15 × production.\n");

        var heldOut = new[] { "S03", "S05", "S07", "S08", "S11", "S12a", "S12b", "S13", "S14", "S15", "S16", "S17" };
        var groups = new (string Title, string[] Ids, bool HeldOut)[]
        {
            ("rapid rise (S11 step +200 through Holding)", ["S11"], true),
            ("rapid fall (S05 step, S03 ramp)", ["S05", "S03"], true),
            ("stable truth (S13–S17)", ["S13", "S14", "S15", "S16", "S17"], true),
            ("water / glycogen / whoosh (S07, S08)", ["S07", "S08"], true),
            ("start-up, TDEE₀ −12 % (S12a/b)", ["S12a", "S12b"], true),
            ("ALL held-out scenarios", heldOut, true),
            ("rapid rise (S02 ramp, S04 step) — calibration scenarios, fresh seeds", ["S02", "S04"], false),
            ("water retention (S06) — calibration scenario, fresh seeds", ["S06"], false),
            ("missing logs / weigh-in gap (S09, S10) — calibration scenarios, fresh seeds", ["S09", "S10"], false),
        };
        Console.WriteLine("## 1. MAE / RMSE (kcal)\n");
        Console.WriteLine("| situation | split | " + string.Join(" | ", names) + " |\n|---|---|" + string.Concat(names.Select(_ => "---|")));
        foreach (var g in groups)
        {
            var ix = Idx(g.Ids).ToList();
            Console.WriteLine($"| {g.Title} | {(g.HeldOut ? "HELD-OUT scenario" : "fresh seeds")} | " + string.Join(" | ", names.Select(nm => $"{F(Mean(ix.Select(i => Mae(worlds[i], est[nm][i]))))} / {F(Mean(ix.Select(i => Rmse(worlds[i], est[nm][i]))))}")) + " |");
        }

        Console.WriteLine("\n## 2. Responsiveness and recovery after a true step (days; mean over seeds)\n");
        Console.WriteLine("| scenario | measure | " + string.Join(" | ", names) + " |\n|---|---|" + string.Concat(names.Select(_ => "---|")));
        foreach (var id in new[] { "S04", "S05", "S11" })
        {
            var ix = Idx([id]).ToList();
            Console.WriteLine($"| {id}{(heldOut.Contains(id) ? " (held-out)" : " (fresh seeds)")} | half-step | " + string.Join(" | ", names.Select(nm => Fn(ix.Select(i => StepTimes(worlds[i], est[nm][i]).Half)))) + " |");
            Console.WriteLine($"| {id} | within ±100 kcal | " + string.Join(" | ", names.Select(nm => Fn(ix.Select(i => StepTimes(worlds[i], est[nm][i]).Recovery)))) + " |");
        }

        Console.WriteLine("\n## 3. False change and stability (truth constant; held-out: S07, S08, S12a/b, S13–S17; plus fresh-seed S01, S06, S09, S10)\n");
        var constHeld = Idx(new[] { "S07", "S08", "S12a", "S12b", "S13", "S14", "S15", "S16", "S17" }).ToList(); var constFresh = Idx(new[] { "S01", "S06", "S09", "S10" }).ToList();
        Console.WriteLine("| measure | " + string.Join(" | ", names) + " |\n|---|" + string.Concat(names.Select(_ => "---|")));
        Console.WriteLine("| false change ≥ 100 kcal / 14 d, held-out | " + string.Join(" | ", names.Select(nm => F(Mean(constHeld.Select(i => FalseChange(worlds[i], est[nm][i], 100))) * 100) + " %")) + " |");
        Console.WriteLine("| false change ≥ 100 kcal / 14 d, fresh seeds | " + string.Join(" | ", names.Select(nm => F(Mean(constFresh.Select(i => FalseChange(worlds[i], est[nm][i], 100))) * 100) + " %")) + " |");
        Console.WriteLine("| false change ≥ 150 kcal / 14 d, held-out | " + string.Join(" | ", names.Select(nm => F(Mean(constHeld.Select(i => FalseChange(worlds[i], est[nm][i], 150))) * 100) + " %")) + " |");
        Console.WriteLine("| stability: mean |Δ|/day (kcal), held-out | " + string.Join(" | ", names.Select(nm => F(Mean(constHeld.Select(i => Flicker(worlds[i], est[nm][i]))), "0.00"))) + " |");

        Console.WriteLine("\n## 4. Acceptance rule applied (all 18 scenario variants for MAE; paired bootstrap vs production)\n");
        var all = Enumerable.Range(0, worlds.Count).ToList(); var change = Idx(["S02", "S03", "S04", "S05", "S11"]).ToList(); var step = Idx(["S04", "S05", "S11"]).ToList();
        var rng = new Random(2026);
        Console.WriteLine("| candidate | (1) ΔMAE overall [95 % CI] | (2) false-change days (≥100) vs production | (3) half-step vs production (S04/S05/S11) | (4) MAE when truth changes vs production | verdict |\n|---|---|---|---|---|---|");
        var pHalf = Mean(step.Select(i => StepTimes(worlds[i], est[names[0]][i]).Half ?? double.NaN)); var pChange = Mean(change.Select(i => Mae(worlds[i], est[names[0]][i])));
        var pFalse = Mean(Idx(ConstantTruth).Select(i => FalseChange(worlds[i], est[names[0]][i], 100)));
        foreach (var nm in names.Skip(1))
        {
            var d = all.Select(i => Mae(worlds[i], est[nm][i]) - Mae(worlds[i], est[names[0]][i])).ToArray();
            var boots = Enumerable.Range(0, 2000).Select(_ => Enumerable.Range(0, d.Length).Average(__ => d[rng.Next(d.Length)])).OrderBy(x => x).ToArray();
            var ok1 = boots[1949] < 0;
            var fc = Mean(Idx(ConstantTruth).Select(i => FalseChange(worlds[i], est[nm][i], 100))); var ok2 = fc < pFalse;
            var half = Mean(step.Select(i => StepTimes(worlds[i], est[nm][i]).Half ?? double.NaN)); var ok3 = half <= 1.25 * pHalf;
            var ch = Mean(change.Select(i => Mae(worlds[i], est[nm][i]))); var ok4 = ch <= 1.15 * pChange;
            Console.WriteLine($"| {nm} | {F(d.Average())} [{F(boots[50])}, {F(boots[1949])}] {(ok1 ? "✔" : "✘")} | {F(fc * 100)} % vs {F(pFalse * 100)} % {(ok2 ? "✔" : "✘")} | {F(half)} d vs {F(pHalf)} d (×{F(half / pHalf, "0.00")}) {(ok3 ? "✔" : "✘")} | {F(ch)} vs {F(pChange)} (×{F(ch / pChange, "0.00")}) {(ok4 ? "✔" : "✘")} | **{(ok1 && ok2 && ok3 && ok4 ? "beats baseline" : "does not beat baseline")}** |");
        }
        return 0;
    }

    public static int Run()
    {
        var calSeeds = Enumerable.Range(1, 20).ToArray(); var valSeeds = Enumerable.Range(101, 20).ToArray();
        var calWorlds = Scenarios.All.Where(s => s.Calibration).SelectMany(s => calSeeds.Select(seed => WorldBuilder.Build(s, seed))).ToList();
        var valWorlds = Scenarios.All.Where(s => s.Id != "S18").SelectMany(s => valSeeds.Select(seed => WorldBuilder.Build(s, seed))).ToList();
        double CalMae(Func<World, double[]> f) => Mean(calWorlds.Select(w => Mae(w, f(w))));

        Console.WriteLine("# Estimator study (single round) — production vs prototypes, synthetic ground truth\n");
        Console.WriteLine($"Worlds: calibration {calWorlds.Count} (S01, S02, S04, S06, S09, S10 × seeds {calSeeds[0]}–{calSeeds[^1]}); evaluation {valWorlds.Count} (18 scenario variants × seeds {valSeeds[0]}–{valSeeds[^1]}; S18 has no data). Errors are measured from day {From}.\n");

        // ---------------- calibration ----------------
        var kf = from s in new[] { 8.0, 16, 32, 64, 128 } from v in new[] { 1.2, 2.0, 3.0, 4.0 } from i in new[] { 250.0, 400, 600, 900 } select new Cand("Kalman", $"σT {s}, σV {v}, σI {i}", w => Estimators.Kalman(w, s, v, i));
        var kfScored = kf.Select(c => (c, m: CalMae(c.Run))).ToList();
        var bestKf = kfScored.OrderBy(x => x.m).First();
        var kfParts = bestKf.c.Label.Split(", "); var bestSigI = double.Parse(kfParts[2][3..], CultureInfo.InvariantCulture);
        var bestSigT = double.Parse(kfParts[0][3..], CultureInfo.InvariantCulture); var bestSigV = double.Parse(kfParts[1][3..], CultureInfo.InvariantCulture);
        var imm = from s1 in new[] { 1.0, 2, 4, 8 } from s2 in new[] { 32.0, 64, 128, 256 } from v in new[] { 1.2, 2.0, 3.0 } from stay in new[] { 0.97, 0.99, 0.995 }
                  select new Cand("IMM", $"σT {s1}/{s2}, σV {v}, stay {stay}", w => Estimators.Imm(w, [s1, s2], [[stay, 1 - stay], [0.05, 0.95]], v, bestSigI));
        var bestImm = imm.Select(c => (c, m: CalMae(c.Run))).OrderBy(x => x.m).First();
        var rs = from wd in new[] { 21, 28, 42, 56, 70 } from b in new[] { 0.025, 0.05, 0.1, 0.2 } select new Cand("RobustSlope", $"W {wd}, β {b}", w => Estimators.RobustSlope(w, wd, b));
        var bestRs = rs.Select(c => (c, m: CalMae(c.Run))).OrderBy(x => x.m).First();
        var pt = from b in new[] { 0.02m, 0.025m, 0.035m, 0.05m, 0.07m, 0.1m, 0.14m, 0.2m } from wd in new[] { 7, 10, 14, 21, 28, 35, 42 } select new Cand("ProdTuned", $"β {b}, W {wd}", w => Estimators.ProductionTuned(w, b, wd));
        var ptScored = pt.Select(c => (c, m: CalMae(c.Run))).ToList();
        var ptBest = ptScored.Min(x => x.m);
        var bestPt = ptScored.Where(x => x.m <= ptBest + 0.5).OrderByDescending(x => int.Parse(x.c.Label[(x.c.Label.LastIndexOf(' ') + 1)..])).First();   // hòa trong 0.5 kcal: chọn cửa sổ DÀI hơn (bảo thủ, ít quá khớp)
        var prod = new Cand("Production", "β 0.1, W 21, trend N 20, 14-d freeze", Estimators.Production);

        Console.WriteLine("## 1. Calibration (parameters chosen on calibration worlds only, objective = MAE)\n");
        Console.WriteLine("| estimator | selected parameters | calibration MAE (kcal) | grid size |\n|---|---|---|---|");
        Console.WriteLine($"| Production | {prod.Label} | {F(CalMae(prod.Run))} | – |");
        Console.WriteLine($"| ProdTuned (production structure, tuned β/W) | {bestPt.c.Label} | {F(bestPt.m)} | {pt.Count()} |");
        Console.WriteLine($"| Kalman (1 model) | {bestKf.c.Label}, σmiss 400, σT₀ 300 | {F(bestKf.m)} | {kf.Count()} |");
        Console.WriteLine($"| IMM (2 models) | {bestImm.c.Label}, σI {bestSigI} (from Kalman) | {F(bestImm.m)} | {imm.Count()} |");
        Console.WriteLine($"| RobustSlope (Theil–Sen on raw weights + EMA) | {bestRs.c.Label} | {F(bestRs.m)} | {rs.Count()} |");
        Console.WriteLine("\nCalibration MAE of the best 5 settings per family:\n");
        Console.WriteLine("| family | setting → MAE |\n|---|---|");
        foreach (var fam in new (string, IEnumerable<Cand>)[] { ("Kalman", kf), ("IMM", imm), ("RobustSlope", rs), ("ProdTuned", pt) })
            Console.WriteLine($"| {fam.Item1} | {string.Join("; ", fam.Item2.Select(c => (c, m: CalMae(c.Run))).OrderBy(x => x.m).Take(5).Select(x => $"{x.c.Label} → {F(x.m)}"))} |");

        var chosen = new[] { prod, new Cand("ProdTuned", bestPt.c.Label, bestPt.c.Run), new Cand("Kalman", bestKf.c.Label, bestKf.c.Run), new Cand("IMM", bestImm.c.Label, bestImm.c.Run), new Cand("RobustSlope", bestRs.c.Label, bestRs.c.Run) };
        var est = chosen.ToDictionary(c => c.Family, c => valWorlds.Select(w => c.Run(w)).ToArray());
        var names = chosen.Select(c => c.Family).ToArray();
        IEnumerable<int> Idx(IEnumerable<string> ids) => Enumerable.Range(0, valWorlds.Count).Where(i => ids.Contains(valWorlds[i].Scenario.Id));
        var all = Enumerable.Range(0, valWorlds.Count).ToList();

        Console.WriteLine("\n## 2. Accuracy on evaluation worlds (MAE / RMSE of the TDEE estimate vs true TDEE, kcal; mean over worlds)\n");
        Console.WriteLine("| group (scenarios) | " + string.Join(" | ", names) + " |\n|---|" + string.Concat(names.Select(_ => "---|")));
        foreach (var (title, ids) in new (string, IEnumerable<string>)[] { ("steady truth (S01, S13–S17)", Steady), ("confounded scale (S06 water, S07 glycogen, S08 whoosh)", Confound), ("data gaps (S09, S10)", Gaps), ("truth changes (S02–S05, S11)", Change), ("start-up, TDEE₀ −12 % (S12a/b)", Start), ("ALL 18 scenario variants", Scenarios.All.Select(s => s.Id).Where(i => i != "S18")) })
        {
            var ix = Idx(ids).ToList();
            Console.WriteLine($"| {title} | " + string.Join(" | ", names.Select(nm => $"{F(Mean(ix.Select(i => Mae(valWorlds[i], est[nm][i]))))} / {F(Mean(ix.Select(i => Rmse(valWorlds[i], est[nm][i]))))}")) + " |");
        }

        Console.WriteLine("\n### Paired comparison with production over all evaluation worlds (ΔMAE = estimator − production; negative = better; 95 % bootstrap CI over worlds)\n");
        Console.WriteLine("| estimator | mean ΔMAE | 95 % CI | worlds better than production |\n|---|---|---|---|");
        var rng = new Random(12345);
        foreach (var nm in names.Skip(1))
        {
            var d = all.Select(i => Mae(valWorlds[i], est[nm][i]) - Mae(valWorlds[i], est["Production"][i])).ToArray();
            var boots = Enumerable.Range(0, 2000).Select(_ => Enumerable.Range(0, d.Length).Average(__ => d[rng.Next(d.Length)])).OrderBy(x => x).ToArray();
            Console.WriteLine($"| {nm} | {F(d.Average())} | [{F(boots[50])}, {F(boots[1949])}] | {F(d.Count(x => x < 0) * 100.0 / d.Length, "0")} % |");
        }

        Console.WriteLine("\n## 3. Forecast error — persistence forecast: |estimate at day d − true TDEE at day d+h|, h = 7 / 14 / 28 days (kcal)\n");
        Console.WriteLine("| set | h | " + string.Join(" | ", names) + " |\n|---|---|" + string.Concat(names.Select(_ => "---|")));
        foreach (var (title, ids) in new (string, IEnumerable<string>)[] { ("all", Scenarios.All.Select(s => s.Id).Where(i => i != "S18")), ("truth changes", Change) })
            foreach (var h in new[] { 7, 14, 28 })
            {
                var ix = Idx(ids).ToList();
                Console.WriteLine($"| {title} | {h} | " + string.Join(" | ", names.Select(nm => F(Mean(ix.Select(i => Forecast(valWorlds[i], est[nm][i], h)))))) + " |");
            }

        Console.WriteLine("\n## 4. Recovery after a true step (days after the event; \"half\" = covered half of the step, \"±100\" = within 100 kcal of the new truth for 7 consecutive days; mean over seeds reaching it)\n");
        Console.WriteLine("| scenario | measure | " + string.Join(" | ", names) + " |\n|---|---|" + string.Concat(names.Select(_ => "---|")));
        foreach (var id in new[] { "S04", "S05", "S11" })
        {
            var ix = Idx([id]).ToList();
            Console.WriteLine($"| {id} | half | " + string.Join(" | ", names.Select(nm => Fn(ix.Select(i => StepTimes(valWorlds[i], est[nm][i]).Half)))) + " |");
            Console.WriteLine($"| {id} | ±100 | " + string.Join(" | ", names.Select(nm => Fn(ix.Select(i => StepTimes(valWorlds[i], est[nm][i]).Recovery)))) + " |");
        }

        Console.WriteLine("\n## 5. False change detection and stability (scenarios whose TRUE expenditure is constant: S01, S06–S10, S12–S17)\n");
        Console.WriteLine("False change = a day where |estimate(d) − estimate(d−14)| exceeds δ while the truth did not move. Stability = mean |day-to-day change| of the estimate.\n");
        Console.WriteLine("| measure | " + string.Join(" | ", names) + " |\n|---|" + string.Concat(names.Select(_ => "---|")));
        var cix = Idx(ConstantTruth).ToList();
        Console.WriteLine("| false change δ = 100 kcal (share of days) | " + string.Join(" | ", names.Select(nm => F(Mean(cix.Select(i => FalseChange(valWorlds[i], est[nm][i], 100))) * 100, "0.0") + " %")) + " |");
        Console.WriteLine("| false change δ = 150 kcal | " + string.Join(" | ", names.Select(nm => F(Mean(cix.Select(i => FalseChange(valWorlds[i], est[nm][i], 150))) * 100, "0.0") + " %")) + " |");
        Console.WriteLine("| stability: mean |Δ| per day (kcal) | " + string.Join(" | ", names.Select(nm => F(Mean(cix.Select(i => Flicker(valWorlds[i], est[nm][i]))), "0.00"))) + " |");

        Console.WriteLine("\n## 6. Initial vs learned TDEE, and Holding\n");
        Console.WriteLine("| measure | " + string.Join(" | ", names) + " |\n|---|---|" + string.Concat(names.Select(_ => "---|")));
        var six = Idx(Start).ToList();
        double Early(string nm, int i) => Mean(Enumerable.Range(0, 28).Select(d => Math.Abs(est[nm][i][d] - valWorlds[i].Truth[d])));
        double HoldingMae(string nm, int i)
            => Mean(Enumerable.Range(From, valWorlds[i].Learned.Count - From).Where(d => valWorlds[i].Learned[d].Status == ExpenditureStatus.Holding).Select(d => Math.Abs(est[nm][i][d] - valWorlds[i].Truth[d])));
        Console.WriteLine("| S12a/b first 28 days: mean |error| (TDEE₀ is −12 % ≈ −300 kcal) | " + string.Join(" | ", names.Select(nm => F(Mean(six.Select(i => Early(nm, i)))))) + " |");
        Console.WriteLine("| S12a/b days 28+: mean |error| | " + string.Join(" | ", names.Select(nm => F(Mean(six.Select(i => Mae(valWorlds[i], est[nm][i])))))) + " |");
        var hix = Idx(["S09", "S10", "S11"]).ToList();
        Console.WriteLine("| MAE on days production is Holding (S09, S10, S11) | " + string.Join(" | ", names.Select(nm => F(Mean(hix.Select(i => HoldingMae(nm, i)))))) + " |");

        Console.WriteLine("\n## 7. MAE per scenario (kcal, mean over seeds)\n");
        Console.WriteLine("| scenario | " + string.Join(" | ", names) + " |\n|---|" + string.Concat(names.Select(_ => "---|")));
        foreach (var s in Scenarios.All.Where(s => s.Id != "S18"))
        {
            var ix = Idx([s.Id]).ToList();
            Console.WriteLine($"| {s.Id} {s.Name} | " + string.Join(" | ", names.Select(nm => F(Mean(ix.Select(i => Mae(valWorlds[i], est[nm][i])))))) + " |");
        }

        // ---------------- Flux evidence (dùng cho mục đánh giá lại Flux) ----------------
        Console.WriteLine("\n## 8. Evidence for the Flux re-assessment\n");
        var within = kfScored.Count(x => x.m <= bestKf.m + 1.0);
        Console.WriteLine($"**Identifiability of the Kalman noise parameters:** {within} of {kfScored.Count} grid settings are within 1 kcal of the best calibration MAE ({F(bestKf.m)}); calibration MAE ranges {F(kfScored.Min(x => x.m))}–{F(kfScored.Max(x => x.m))} over the whole grid. A flat objective means the data cannot pin σT / σV / σI down.\n");
        var xs = new Dictionary<int, List<double>>(); var ys = new Dictionary<int, List<double>>(); var sdStable = new List<double>(); var sdChange = new List<double>();
        foreach (var win in new[] { 14, 28, 42 }) { xs[win] = []; ys[win] = []; }
        foreach (var w in valWorlds)
        {
            var sd = new double[w.Learned.Count]; Estimators.Kalman(w, bestSigT, bestSigV, bestSigI, sdOut: sd);
            var m14 = Metrics.ChangeMagnitude(w.Truth, 14);
            foreach (var win in new[] { 14, 28, 42 }) { var m = Metrics.ChangeMagnitude(w.Truth, win); for (var d = From; d < w.Learned.Count; d++) { xs[win].Add(sd[d]); ys[win].Add(m[d]); } }
            for (var d = From; d < w.Learned.Count; d++) { if (m14[d] <= Metrics.StableMax) sdStable.Add(sd[d]); if (m14[d] >= Metrics.ChangeMin) sdChange.Add(sd[d]); }
        }
        Console.WriteLine("**Kalman posterior sd of the TDEE vs the TRUE change magnitude** (pooled over all evaluation worlds, days ≥ 28):\n");
        Console.WriteLine("| truth window | Spearman ρ(sd, true change) |\n|---|---|");
        foreach (var win in new[] { 14, 28, 42 }) Console.WriteLine($"| {win} d | {F(Metrics.Spearman(xs[win], ys[win]), "0.000")} |");
        Console.WriteLine($"\nMean posterior sd: {F(Mean(sdStable))} kcal on days where the truth is stable, {F(Mean(sdChange))} kcal where it is changing (a principled uncertainty that barely depends on whether the truth moved).");
        return 0;
    }
}
