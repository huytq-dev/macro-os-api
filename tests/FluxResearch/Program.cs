// Nghiên cứu Flux (Phase 2–3 + 6): mô phỏng tổng hợp, hiệu chỉnh CHỈ trên tập calibration, kiểm định trên kịch bản giữ lại + seed mới. Không DB, không API, không đổi production.
// Chạy: dotnet run --project tests/FluxResearch -c Release -- all   (in markdown ra stdout)
using System.Globalization;
using Application;
using Domain;
using FluxResearch;

if (args.FirstOrDefault() == "study") return EstimatorStudy.Run();   // một vòng nghiên cứu bộ ước lượng (xem EstimatorStudy.cs)
if (args.FirstOrDefault() == "closing") return EstimatorStudy.RunClosing();   // đánh giá đóng: β 0.035–0.07, cửa sổ 14, quy tắc chấp nhận chốt trước

var kcal = WorldBuilder.KcalPerKg;
var calSeeds = Enumerable.Range(1, 20).ToArray();
var valSeeds = Enumerable.Range(101, 20).ToArray();
const decimal NomBeta = 0.1m;
const int NomWin = 21;
const double Kappa = 1.5;     // MACROOS POLICY: ứng viên "khả dĩ" nếu MAE ≤ Kappa × MAE của production trên tập hiệu chỉnh

string F(double v, string fmt = "0.0") => double.IsNaN(v) ? "-" : v.ToString(fmt, CultureInfo.InvariantCulture);
string Fn(IEnumerable<double?> xs) { var all = xs.ToList(); var l = all.Where(x => x.HasValue).Select(x => x!.Value).ToList(); return l.Count == 0 ? "-" : $"{l.Average():0.0} ({l.Count}/{all.Count})"; }
double MeanOf(IEnumerable<double> xs) { var l = xs.Where(x => !double.IsNaN(x)).ToList(); return l.Count == 0 ? double.NaN : l.Average(); }
string K(World w) => $"{w.Scenario.Id}/{w.Seed}";

var cache = new Dictionary<string, decimal[]>();
decimal[] Path(World w, decimal beta, int win)
{
    var key = $"{K(w)}|{beta}|{win}";
    if (!cache.TryGetValue(key, out var p)) cache[key] = p = ExpenditureFluxCalculator.Trajectory(w.Observations, w.Learned, w.Start, new FluxCandidateSpec("c", beta, win), kcal);
    return p;
}
var magCache = new Dictionary<string, double[]>();
double[] Mag(World w) => magCache.TryGetValue(K(w), out var m) ? m : magCache[K(w)] = Metrics.ChangeMagnitude(w.Truth);
BandSet BandOf(World w, string name, IReadOnlyList<FluxCandidateSpec> specs)
    => Metrics.FromPoints(name, ExpenditureFluxCalculator.Band(w.Learned, specs.Select(s => Path(w, s.Beta, s.WindowDays)).ToList()));
double PathMae(IEnumerable<World> worlds, decimal beta, int win)
    => MeanOf(worlds.SelectMany(w => Enumerable.Range(Metrics.Warmup, w.Learned.Count - Metrics.Warmup).Where(d => w.Learned[d].Status != ExpenditureStatus.Calibrating).Select(d => Math.Abs((double)Path(w, beta, win)[d] - w.Truth[d]))));

(double Rho, double Mean, double Stable, double Change) Objective(IEnumerable<World> worlds, IReadOnlyList<FluxCandidateSpec> specs) => ObjectiveOf(worlds, w => BandOf(w, "x", specs));

// C4 (alternativa): biên độ dịch chuyển của CHÍNH TDEE đã học trong H ngày gần nhất (nhân quả; Holding giữ nguyên dải cuối vì không có quan sát mới)
BandSet MotionBand(World w, int h)
{
    var n = w.Learned.Count; var lower = new double?[n]; var upper = new double?[n]; var everUpdating = false;
    for (var d = 0; d < n; d++)
    {
        var st = w.Learned[d].Status;
        if (st == ExpenditureStatus.Updating) everUpdating = true;
        if (st == ExpenditureStatus.Calibrating || (st == ExpenditureStatus.Holding && !everUpdating)) continue;
        if (st == ExpenditureStatus.Holding && d > 0 && lower[d - 1] is not null) { lower[d] = lower[d - 1]; upper[d] = upper[d - 1]; continue; }
        var vals = Enumerable.Range(Math.Max(0, d - h), Math.Min(h, d) + 1).Where(s => w.Learned[s].Status != ExpenditureStatus.Calibrating).Select(s => w.Learned[s].TdeeKcal).ToList();
        lower[d] = (double)Math.Floor(vals.Min()); upper[d] = (double)Math.Ceiling(vals.Max());
    }
    return new BandSet($"C4 motion H={h}", lower, upper);
}

// C5 (alternativa): dời chỗ THUẦN của TDEE đã học so với H ngày trước (hai điểm, triệt bớt nhiễu hơn khoảng min–max); Holding giữ dải cuối
BandSet NetBand(World w, int h)
{
    var n = w.Learned.Count; var lower = new double?[n]; var upper = new double?[n]; var everUpdating = false;
    for (var d = 0; d < n; d++)
    {
        var st = w.Learned[d].Status;
        if (st == ExpenditureStatus.Updating) everUpdating = true;
        if (st == ExpenditureStatus.Calibrating || (st == ExpenditureStatus.Holding && !everUpdating)) continue;
        if (st == ExpenditureStatus.Holding && d > 0 && lower[d - 1] is not null) { lower[d] = lower[d - 1]; upper[d] = upper[d - 1]; continue; }
        var a = w.Learned[d].TdeeKcal; var b = w.Learned[Math.Max(0, d - h)].TdeeKcal;
        lower[d] = (double)Math.Floor(Math.Min(a, b)); upper[d] = (double)Math.Ceiling(Math.Max(a, b));
    }
    return new BandSet($"C5 net H={h}", lower, upper);
}

(double Rho, double Mean, double Stable, double Change) ObjectiveOf(IEnumerable<World> worlds, Func<World, BandSet> bandOf)
{
    var xs = new List<double>(); var ys = new List<double>(); var st = new List<double>(); var ch = new List<double>();
    foreach (var w in worlds)
    {
        var band = bandOf(w); var m = Mag(w);
        for (var d = Metrics.Warmup; d < w.Learned.Count; d++)
        {
            if (band.Lower[d] is null) continue;
            var width = band.Upper[d]!.Value - band.Lower[d]!.Value;
            xs.Add(width); ys.Add(m[d]);
            if (m[d] <= Metrics.StableMax) st.Add(width);
            if (m[d] >= Metrics.ChangeMin) ch.Add(width);
        }
    }
    return (Metrics.Spearman(xs, ys), xs.Average(), st.Count == 0 ? double.NaN : st.Average(), ch.Count == 0 ? double.NaN : ch.Average());
}

Console.WriteLine("# Flux research run (MACROOS_FLUX_V1 candidate) — synthetic ground truth\n");
Console.WriteLine($"- production estimator replicated with the production functions: window W={ProductionReplica.WindowDays}, EMA β={ProductionReplica.Beta}, trend EMA N={WorldBuilder.TrendDays}, kcal/kg={kcal}, Calibrating freeze {ProductionReplica.CalibratingDays} d (default).");
Console.WriteLine($"- calibration scenarios: {string.Join(", ", Scenarios.All.Where(s => s.Calibration).Select(s => s.Id))} × seeds {calSeeds[0]}–{calSeeds[^1]}; held-out: {string.Join(", ", Scenarios.All.Where(s => !s.Calibration).Select(s => s.Id))}; validation seeds {valSeeds[0]}–{valSeeds[^1]} (never used for calibration).\n");

// ---------------- CALIBRATION ----------------
var calWorlds = Scenarios.All.Where(s => s.Calibration).SelectMany(s => calSeeds.Select(seed => WorldBuilder.Build(s, seed))).ToList();
var betas = new[] { 0.025m, 0.035m, 0.05m, 0.07m, 0.1m, 0.14m, 0.2m, 0.3m };
var wins = new[] { 7, 10, 14, 21, 28, 35, 42, 56 };
var nominalMae = PathMae(calWorlds, NomBeta, NomWin);
Console.WriteLine("## 1. Plausibility screening (calibration scenarios only)\n");
Console.WriteLine($"A candidate trajectory is *plausible* when an estimator with those settings would itself be acceptable: MAE vs ground truth ≤ κ × MAE of production (κ = {Kappa}, MACROOS POLICY, needs approval). Production (β {NomBeta}, W {NomWin}) MAE = {F(nominalMae, "0.0")} kcal.\n");
Console.WriteLine("| axis | value | MAE (kcal) | MAE / production | plausible |\n|---|---|---|---|---|");
var acceptedBeta = new List<decimal>(); var acceptedWin = new List<int>();
foreach (var b in betas) { var m = PathMae(calWorlds, b, NomWin); var ok = m <= Kappa * nominalMae; if (ok) acceptedBeta.Add(b); Console.WriteLine($"| β (W={NomWin}) | {b} | {F(m)} | {F(m / nominalMae, "0.00")} | {(ok ? "yes" : "no")} |"); }
foreach (var wd in wins) { var m = PathMae(calWorlds, NomBeta, wd); var ok = m <= Kappa * nominalMae; if (ok) acceptedWin.Add(wd); Console.WriteLine($"| W (β={NomBeta}) | {wd} | {F(m)} | {F(m / nominalMae, "0.00")} | {(ok ? "yes" : "no")} |"); }

var slow = acceptedBeta.Where(b => b < NomBeta).ToList(); var fast = acceptedBeta.Where(b => b > NomBeta).ToList();
var shortW = acceptedWin.Where(w => w < NomWin).ToList(); var longW = acceptedWin.Where(w => w > NomWin).ToList();
Console.WriteLine($"\nPlausible slow β: {string.Join(", ", slow)}; fast β: {string.Join(", ", fast)}; short W: {string.Join(", ", shortW)}; long W: {string.Join(", ", longW)}.\n");

FluxCandidateSpec S(string n, decimal b, int w) => new(n, b, w);
var c1 = (from a in slow from b in fast select (Specs: (IReadOnlyList<FluxCandidateSpec>)[S("slow", a, NomWin), S("fast", b, NomWin)], Label: $"β {a}/{b}")).ToList();
var c2 = (from a in shortW from b in longW select (Specs: (IReadOnlyList<FluxCandidateSpec>)[S("short", NomBeta, a), S("long", NomBeta, b)], Label: $"W {a}/{b}")).ToList();
var c3 = (from a in slow from b in fast from s in shortW from l in longW select (Specs: (IReadOnlyList<FluxCandidateSpec>)[S("slow", a, NomWin), S("fast", b, NomWin), S("short", NomBeta, s), S("long", NomBeta, l)], Label: $"β {a}/{b} + W {s}/{l}")).ToList();

(IReadOnlyList<FluxCandidateSpec> Specs, string Label) Select(string title, List<(IReadOnlyList<FluxCandidateSpec> Specs, string Label)> combos)
{
    var scored = combos.Select(c => (c.Specs, c.Label, Obj: Objective(calWorlds, c.Specs))).OrderByDescending(x => x.Obj.Rho).ToList();
    var best = scored[0].Obj.Rho;
    var chosen = scored.Where(x => best - x.Obj.Rho <= 0.005).OrderBy(x => x.Obj.Mean).First();   // ngang nhau (≤ 0.005) thì chọn dải hẹp hơn
    Console.WriteLine($"### {title}: top 5 of {scored.Count} by Spearman(width, true change magnitude), calibration only\n");
    Console.WriteLine("| combination | Spearman ρ | mean width | width in stable truth | width when truth changes |\n|---|---|---|---|---|");
    foreach (var x in scored.Take(5)) Console.WriteLine($"| {x.Label} | {F(x.Obj.Rho, "0.000")} | {F(x.Obj.Mean)} | {F(x.Obj.Stable)} | {F(x.Obj.Change)} |");
    Console.WriteLine($"\n**Selected {title}: {chosen.Label}** (ρ {F(chosen.Obj.Rho, "0.000")}; ties within 0.005 resolved to the narrower band).\n");
    return (chosen.Specs, chosen.Label);
}
Console.WriteLine("## 2. Calibration (objective: the band width must rank-track the TRUE change magnitude; scale-free, so it does not tune the band to look like any screenshot)\n");
var sel1 = Select("C1 responsiveness envelope (slow/fast β)", c1);
var sel2 = Select("C2 short/long window divergence", c2);
var sel3 = Select("C3 combined envelope", c3);

Console.WriteLine("### C4 motion envelope (range of the learned TDEE over the last H days): calibration only\n");
Console.WriteLine("| H (days) | Spearman ρ | mean width | width in stable truth | width when truth changes |\n|---|---|---|---|---|");
var c4 = new[] { 7, 10, 14, 21, 28, 42 }.Select(h => (H: h, O: ObjectiveOf(calWorlds, w => MotionBand(w, h)))).ToList();
foreach (var x in c4) Console.WriteLine($"| {x.H} | {F(x.O.Rho, "0.000")} | {F(x.O.Mean)} | {F(x.O.Stable)} | {F(x.O.Change)} |");
var bestC4 = c4.OrderByDescending(x => x.O.Rho).First(); var selH = c4.Where(x => bestC4.O.Rho - x.O.Rho <= 0.005).OrderBy(x => x.O.Mean).First().H;
Console.WriteLine($"\n**Selected C4: H = {selH} days** (ρ {F(c4.First(x => x.H == selH).O.Rho, "0.000")}).\n");

Console.WriteLine("### C5 net-displacement band (|learned TDEE(t) − learned TDEE(t−H)|): calibration only\n");
Console.WriteLine("| H (days) | Spearman ρ | mean width | width in stable truth | width when truth changes |\n|---|---|---|---|---|");
var c5 = new[] { 7, 14, 21, 28, 42 }.Select(h => (H: h, O: ObjectiveOf(calWorlds, w => NetBand(w, h)))).ToList();
foreach (var x in c5) Console.WriteLine($"| {x.H} | {F(x.O.Rho, "0.000")} | {F(x.O.Mean)} | {F(x.O.Stable)} | {F(x.O.Change)} |");
var bestC5 = c5.OrderByDescending(x => x.O.Rho).First(); var selNet = c5.Where(x => bestC5.O.Rho - x.O.Rho <= 0.005).OrderBy(x => x.O.Mean).First().H;
Console.WriteLine($"\n**Selected C5: H = {selNet} days** (ρ {F(c5.First(x => x.H == selNet).O.Rho, "0.000")}).\n");

// Sensitivity: tải kappa
Console.WriteLine("### Sensitivity of the plausibility bound κ (calibration only)\n");
Console.WriteLine("| κ | plausible β | plausible W | C3 selection |\n|---|---|---|---|");
foreach (var k in new[] { 1.25, 1.5, 2.0 })
{
    var ab = betas.Where(b => PathMae(calWorlds, b, NomWin) <= k * nominalMae).ToList(); var aw = wins.Where(w => PathMae(calWorlds, NomBeta, w) <= k * nominalMae).ToList();
    var cs = (from a in ab.Where(x => x < NomBeta) from b in ab.Where(x => x > NomBeta) from s in aw.Where(x => x < NomWin) from l in aw.Where(x => x > NomWin)
              select (Label: $"β {a}/{b} + W {s}/{l}", O: Objective(calWorlds, [S("slow", a, NomWin), S("fast", b, NomWin), S("short", NomBeta, s), S("long", NomBeta, l)]))).OrderByDescending(x => x.O.Rho).ToList();
    Console.WriteLine($"| {k} | {string.Join(",", ab)} | {string.Join(",", aw)} | {(cs.Count == 0 ? "-" : $"{cs[0].Label} (ρ {F(cs[0].O.Rho, "0.000")}, mean width {F(cs[0].O.Mean)})")} |");
}

// τ (ngưỡng "dải đã nở"): 1.5 × median độ rộng ở ngày TDEE thật ổn định của S01, tập hiệu chỉnh, từng tập dải
double Tau(Func<World, BandSet> bandOf)
{
    var widths = new List<double>();
    foreach (var w in calWorlds.Where(w => w.Scenario.Id == "S01"))
    {
        var b = bandOf(w); var m = Mag(w);
        for (var d = Metrics.Warmup; d < w.Learned.Count; d++) if (b.Lower[d] is not null && m[d] <= Metrics.StableMax) widths.Add(b.Upper[d]!.Value - b.Lower[d]!.Value);
    }
    return 1.5 * Metrics.Median(widths);
}
var sets = new (string Name, Func<World, BandSet> Band)[]
{
    ("L0 legacy ±37", Metrics.Legacy),
    ("C1 β-envelope", w => BandOf(w, "C1", sel1.Specs)),
    ("C2 W-divergence", w => BandOf(w, "C2", sel2.Specs)),
    ("C3 combined", w => BandOf(w, "C3", sel3.Specs)),
    ("C4 motion", w => MotionBand(w, selH)),
    ("C5 net displacement", w => NetBand(w, selNet)),
};
var taus = sets.ToDictionary(s => s.Name, s => Tau(s.Band));
Console.WriteLine($"\n\"Expanded\" threshold τ = 1.5 × median stable-truth width on S01 (calibration seeds): {string.Join("; ", taus.Select(t => $"{t.Key} {F(t.Value)} kcal"))}.\n");

// ---------------- VALIDATION ----------------
Console.WriteLine("## 3. Validation on HELD-OUT scenarios (fresh seeds, parameters frozen)\n");
var results = new Dictionary<(string Scenario, string Set), List<WorldMetrics>>();
var valWorlds = Scenarios.All.SelectMany(s => valSeeds.Select(seed => WorldBuilder.Build(s, seed))).ToList();
foreach (var w in valWorlds)
    foreach (var set in sets)
    {
        var m = Metrics.Evaluate(w, set.Band(w), taus[set.Name], Mag(w));
        var key = (w.Scenario.Id, set.Name);
        if (!results.TryGetValue(key, out var l)) results[key] = l = [];
        l.Add(m);
    }
foreach (var set in sets)
{
    Console.WriteLine($"### {set.Name} (τ {F(taus[set.Name])} kcal) — mean over {valSeeds.Length} seeds\n");
    Console.WriteLine("| scenario | split | learned MAE | width mean | width stable | width change | false expansion | onset lag (d) | recovery (d) | flicker/day | truth in band % |\n|---|---|---|---|---|---|---|---|---|---|---|");
    foreach (var s in Scenarios.All)
    {
        var l = results[(s.Id, set.Name)];
        Console.WriteLine($"| {s.Id} {s.Name} | {(s.Calibration ? "calibration(fresh seeds)" : "HELD-OUT")} | {F(MeanOf(l.Select(x => x.LearnedMae)))} | {F(MeanOf(l.Select(x => x.WidthMean)))} | {F(MeanOf(l.Select(x => x.WidthStable)))} | {F(MeanOf(l.Select(x => x.WidthChange)))} | {F(MeanOf(l.Select(x => x.FalseExpansion)) * 100, "0")}% | {Fn(l.Select(x => x.OnsetLagDays))} | {Fn(l.Select(x => x.RecoveryDays))} | {F(MeanOf(l.Select(x => x.Flicker)), "0.0")} | {F(MeanOf(l.Select(x => x.TruthInBand)) * 100, "0")} |");
    }
    Console.WriteLine();
}
Console.WriteLine("### Learned estimator (production) responsiveness on step scenarios — days until the learned TDEE has covered half of the true step\n");
Console.WriteLine("| scenario | days to half-step (mean, seeds reaching it) |\n|---|---|");
foreach (var s in Scenarios.All.Where(s => s.EventDay is not null)) Console.WriteLine($"| {s.Id} {s.Name} | {Fn(results[(s.Id, "C3 combined")].Select(x => x.LearnedHalfDays))} |");

Console.WriteLine("\n### Pooled Spearman(width, true change magnitude) on held-out data (parameters were NOT fitted on it)\n");
Console.WriteLine("| set | held-out scenarios ρ | calibration scenarios, fresh seeds ρ | held-out mean width | held-out width in stable truth | held-out width when truth changes |\n|---|---|---|---|---|---|");
foreach (var set in sets)
{
    (double, double, double, double) Pool(IEnumerable<World> ws)
    {
        var xs = new List<double>(); var ys = new List<double>(); var st = new List<double>(); var ch = new List<double>();
        foreach (var w in ws) { var b = set.Band(w); var m = Mag(w); for (var d = Metrics.Warmup; d < w.Learned.Count; d++) { if (b.Lower[d] is null) continue; var wd = b.Upper[d]!.Value - b.Lower[d]!.Value; xs.Add(wd); ys.Add(m[d]); if (m[d] <= Metrics.StableMax) st.Add(wd); if (m[d] >= Metrics.ChangeMin) ch.Add(wd); } }
        return (Metrics.Spearman(xs, ys), xs.Count == 0 ? double.NaN : xs.Average(), st.Count == 0 ? double.NaN : st.Average(), ch.Count == 0 ? double.NaN : ch.Average());
    }
    var held = Pool(valWorlds.Where(w => !w.Scenario.Calibration)); var fresh = Pool(valWorlds.Where(w => w.Scenario.Calibration));
    Console.WriteLine($"| {set.Name} | {F(held.Item1, "0.000")} | {F(fresh.Item1, "0.000")} | {F(held.Item2)} | {F(held.Item3)} | {F(held.Item4)} |");
}

Console.WriteLine("\n### Sensitivity to how \"true change magnitude\" is windowed (held-out pooled Spearman; primary definition = 14 days, fixed before looking at results)\n");
Console.WriteLine("| set | truth window 14 d | 28 d | 42 d |\n|---|---|---|---|");
foreach (var set in sets)
{
    string RhoAt(int win)
    {
        var xs = new List<double>(); var ys = new List<double>();
        foreach (var w in valWorlds.Where(w => !w.Scenario.Calibration)) { var b = set.Band(w); var m = Metrics.ChangeMagnitude(w.Truth, win); for (var d = Metrics.Warmup; d < w.Learned.Count; d++) if (b.Lower[d] is not null) { xs.Add(b.Upper[d]!.Value - b.Lower[d]!.Value); ys.Add(m[d]); } }
        return F(Metrics.Spearman(xs, ys), "0.000");
    }
    Console.WriteLine($"| {set.Name} | {RhoAt(14)} | {RhoAt(28)} | {RhoAt(42)} |");
}

Console.WriteLine("\n### Noise sensitivity: is the weak rank-tracking a signal-to-noise limit? (calibration scenarios, parameters frozen from the realistic-noise calibration)\n");
Console.WriteLine("| noise scale | production learned MAE | C1 ρ | C2 ρ | C3 ρ | C4 ρ | C5 ρ |\n|---|---|---|---|---|---|---|");
foreach (var scale in new[] { 0.0, 0.25, 0.5, 1.0 })
{
    var ws = Scenarios.All.Where(s => s.Calibration).SelectMany(s => calSeeds.Select(seed => WorldBuilder.Build(s with { NoiseScale = scale }, seed + 500))).ToList();
    var mae = MeanOf(ws.SelectMany(w => Enumerable.Range(Metrics.Warmup, w.Learned.Count - Metrics.Warmup).Where(d => w.Learned[d].Status != ExpenditureStatus.Calibrating).Select(d => Math.Abs((double)w.Learned[d].TdeeKcal - w.Truth[d]))));
    string R(Func<World, BandSet> f) => F(ObjectiveOf(ws, f).Rho, "0.000");
    Console.WriteLine($"| {scale} | {F(mae)} | {R(w => BandOf(w, "x", sel1.Specs))} | {R(w => BandOf(w, "x", sel2.Specs))} | {R(w => BandOf(w, "x", sel3.Specs))} | {R(w => MotionBand(w, selH))} | {R(w => NetBand(w, selNet))} |");
}

// ---------------- INVARIANTS ----------------
Console.WriteLine("\n## 4. Invariants and rules (C3 combined, validation seeds)\n");
var pass = 0; var fail = 0;
void Check(bool ok, string msg) { if (ok) pass++; else fail++; Console.WriteLine($"- {(ok ? "PASS" : "FAIL")} {msg}"); }
FluxCalculationPolicy Pol(IReadOnlyList<FluxCandidateSpec> specs) => new("RESEARCH", specs);
IReadOnlyList<FluxPoint> Flux(World w) => ExpenditureFluxCalculator.Compute(w.Observations, w.Learned, w.Start, Pol(sel3.Specs), kcal);

var contained = valWorlds.All(w => { var f = Flux(w); return f.Zip(w.Learned).All(p => p.First.LowerKcal is null || (p.First.LowerKcal <= Math.Round(p.Second.TdeeKcal, 0, MidpointRounding.AwayFromZero) && Math.Round(p.Second.TdeeKcal, 0, MidpointRounding.AwayFromZero) <= p.First.UpperKcal)); });
Check(contained, $"Lower ≤ learned TDEE ≤ Upper on every day of all {valWorlds.Count} validation worlds");
Check(valWorlds.All(w => { var f = Flux(w); var g = Flux(w); return f.Zip(g).All(p => p.First.LowerKcal == p.Second.LowerKcal && p.First.UpperKcal == p.Second.UpperKcal); }), "deterministic: two runs give identical bands");

var s12a = valWorlds.First(w => w.Scenario.Id == "S12a" && w.Seed == valSeeds[0]); var f12a = Flux(s12a);
Check(f12a.Zip(s12a.Learned).All(p => p.Second.Status != ExpenditureStatus.Calibrating || p.First.LowerKcal is null) && f12a.Zip(s12a.Learned).Any(p => p.Second.Status == ExpenditureStatus.Updating && p.First.LowerKcal is not null),
    $"S12a Calibrating days (frozen 14 d, {s12a.Learned.Count(l => l.Status == ExpenditureStatus.Calibrating)} days) have NO band; the band appears once the status is Updating");
var s12b = valWorlds.First(w => w.Scenario.Id == "S12b" && w.Seed == valSeeds[0]); var f12b = Flux(s12b);
Check(f12b.Zip(s12b.Learned).All(p => p.Second.Status != ExpenditureStatus.Calibrating || p.First.LowerKcal is null), $"S12b (learn from day 1): Calibrating = no band ({s12b.Learned.Count(l => l.Status == ExpenditureStatus.Calibrating)} Calibrating days until the first Updating)");
Check(Flux(valWorlds.First(w => w.Scenario.Id == "S18")).All(p => p.LowerKcal is null && p.UpperKcal is null), "S18 no valid observations: every day null");

var s11 = valWorlds.First(w => w.Scenario.Id == "S11" && w.Seed == valSeeds[0]); var f11 = Flux(s11);
var holdIdx = Enumerable.Range(1, s11.Learned.Count - 1).Where(i => s11.Learned[i].Status == ExpenditureStatus.Holding && s11.Learned[i - 1].Status == ExpenditureStatus.Holding).ToList();
Check(holdIdx.Count > 0 && holdIdx.All(i => f11[i].LowerKcal == f11[i - 1].LowerKcal && f11[i].UpperKcal == f11[i - 1].UpperKcal && f11[i].State == FluxBandState.Holding),
    $"S11 Holding: {holdIdx.Count} consecutive Holding days have an UNCHANGED band (no new observation ⇒ no candidate update), state = Holding");

var s04 = valWorlds.First(w => w.Scenario.Id == "S04" && w.Seed == valSeeds[0]); var full = Flux(s04);
var causal = new[] { 45, 80, 100, 120 }.All(cut =>
{
    var obs = s04.Observations.Take(cut + 1).ToList(); var lrn = s04.Learned.Take(cut + 1).ToList();
    var part = ExpenditureFluxCalculator.Compute(obs, lrn, s04.Start, Pol(sel3.Specs), kcal);
    return part.Zip(full).All(p => p.First.LowerKcal == p.Second.LowerKcal && p.First.UpperKcal == p.Second.UpperKcal);
});
Check(causal, "causality (S04): the band at day d computed with data truncated at d equals the band computed with all later data (no future data used)");

Check(Flux(valWorlds.First(w => w.Scenario.Id == "S13" && w.Seed == valSeeds[0])).Zip(Flux(valWorlds.First(w => w.Scenario.Id == "S01" && w.Seed == valSeeds[0]))).All(p => p.First.LowerKcal == p.Second.LowerKcal && p.First.UpperKcal == p.Second.UpperKcal),
    "S13 Static mode: identical input ⇒ band identical to S01; Flux is a function of the LEARNED estimator only (a static value of 2300 does not enter it)");
Check(Flux(valWorlds.First(w => w.Scenario.Id == "S15" && w.Seed == valSeeds[0])).Zip(Flux(valWorlds.First(w => w.Scenario.Id == "S01" && w.Seed == valSeeds[0]))).All(p => p.First.LowerKcal == p.Second.LowerKcal && p.First.UpperKcal == p.Second.UpperKcal),
    "S15 program change without TDEE reset: program/goal are not inputs ⇒ band identical to S01");

var s16 = Scenarios.All.First(s => s.Id == "S16");
var before = WorldBuilder.Build(s16, valSeeds[0], withBackfilled: false); var after = WorldBuilder.Build(s16, valSeeds[0], withBackfilled: true);
var fb = Flux(before); var fa = Flux(after);
Check(Enumerable.Range(0, 40).All(i => fb[i].LowerKcal == fa[i].LowerKcal && fb[i].UpperKcal == fa[i].UpperKcal) && Enumerable.Range(40, fa.Count - 40).Any(i => fb[i].LowerKcal != fa[i].LowerKcal || fb[i].UpperKcal != fa[i].UpperKcal),
    $"S16 backdated log (days 40–46 entered late): days before day 40 are untouched; {Enumerable.Range(40, fa.Count - 40).Count(i => fb[i].LowerKcal != fa[i].LowerKcal || fb[i].UpperKcal != fa[i].UpperKcal)} later days are recomputed (a derived rebuild, as production does)");

var s14 = valWorlds.Where(w => w.Scenario.Id == "S14").ToList(); var s01v = valWorlds.Where(w => w.Scenario.Id == "S01").ToList();
Check(MeanOf(s14.Select(w => results[("S14", "C3 combined")].Average(x => x.FalseExpansion))) <= MeanOf(results[("S01", "C3 combined")].Select(x => x.FalseExpansion)) + 0.25,
    "S14 goal change (intake changes, TDEE does not): band does not blow up because of the intake change alone (false expansion within 25 pts of S01)");
Console.WriteLine($"\nINVARIANTS: {pass} PASS, {fail} FAIL");

// Dữ liệu từng ngày của MỘT seed (validation, seed đầu) mỗi kịch bản: sự thật nền, TDEE đã học, 4 ứng viên, Lower/Upper (C3)
var outDir = System.IO.Path.Combine("Docs", "Design", "Flux", "E_data"); Directory.CreateDirectory(outDir);
foreach (var s in Scenarios.All)
{
    var w = valWorlds.First(x => x.Scenario.Id == s.Id && x.Seed == valSeeds[0]); var f = Flux(w);
    var p = sel3.Specs.Select(sp => Path(w, sp.Beta, sp.WindowDays)).ToList();
    using var sw = new StreamWriter(System.IO.Path.Combine(outDir, $"{s.Id}.csv"));
    sw.WriteLine($"day,date,true_tdee,learned_tdee,status,{string.Join(",", sel3.Specs.Select(x => x.Name))},lower,upper,state");
    for (var d = 0; d < w.Learned.Count; d++)
        sw.WriteLine(string.Join(",", [d.ToString(), w.Learned[d].Date.ToString("yyyy-MM-dd"), w.Truth[d].ToString("0.0", CultureInfo.InvariantCulture), w.Learned[d].TdeeKcal.ToString(CultureInfo.InvariantCulture), w.Learned[d].Status.ToString(),
            .. p.Select(x => x[d].ToString(CultureInfo.InvariantCulture)), f[d].LowerKcal?.ToString(CultureInfo.InvariantCulture) ?? "", f[d].UpperKcal?.ToString(CultureInfo.InvariantCulture) ?? "", f[d].State.ToString()]));
}
Console.WriteLine($"\nPer-scenario day-by-day data (seed {valSeeds[0]}): `{outDir}/<scenario>.csv` (true TDEE, learned TDEE + status, 4 candidate trajectories, Lower/Upper, band state).");

Console.WriteLine("\n## 5. Selected parameters (to be copied into FluxCalculationPolicy.V1 once approved)\n");
Console.WriteLine("```csharp");
foreach (var sp in sel3.Specs) Console.WriteLine($"new FluxCandidateSpec(\"{sp.Name}\", {sp.Beta}m, {sp.WindowDays}),");
Console.WriteLine("```");
return fail == 0 ? 0 : 1;
