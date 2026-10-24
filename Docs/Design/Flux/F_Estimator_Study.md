# Report F — Expenditure estimator study (one bounded round) + Flux re-assessment + open verification questions

Date 2026-10-23. **No production change**: the learned TDEE algorithm, the G0 state machine, snapshots, targets, check-ins, Calories/Macros/Strategy are untouched; no historical rebuild; no migration; no DB; nothing committed or deployed. Everything below is **synthetic** (known ground truth) and depends on the simulator assumptions in Report E §1. Reproduce: `dotnet run --project tests/FluxResearch -c Release -- study` from `BE/` (≈ 35 s). Full output: `F_Estimator_Study_Output.md`.

## 0. What changed in the code this round (decision "giữ ẩn Flux")
`GET /api/expenditure/series` and `GET /api/expenditure` now return `fluxLowKcal = fluxHighKcal = null` for every day; the constant ±37 band (`ExpenditureCalculator.FluxHalfWidth`) and the `Algorithm:FluxTrendErrorKg` option were removed. Snapshot data is untouched; no status/model marker exists in the DTO, so none was added (just null). `ExpenditureSeriesDay.FluxLowKcal/FluxHighKcal` became `decimal?` (same JSON names). **Compatibility confirmed by grep:** FE types already declare `fluxLowKcal?: number | null` (`expenditure-series.types.ts:11-12`, `dashboard.types.ts:13-14`), no component reads it (`check-contract.mjs:1780,1832` assert exactly that and test the null case). AlgorithmChecks: 842 PASS (the two arithmetic checks for ±37 were replaced by "all days null"). The research engine in `src/Application/Features/Expenditure/Flux/` stays, unwired. A BE process started before this change keeps the old band until restarted.

## 1. Method
- **Estimators (same observations, same scenarios as Report E; 5 methods):**
  1. **Production** — the replicated engine (window 21 on a 20-day EMA trend, EMA β 0.1, G0 states, 14-day freeze).
  2. **ProdTuned** — the *production structure* (production functions, production Updating/Holding gating) with β and W tuned.
  3. **Kalman** — 2-state linear filter on [true weight, TDEE]: `W' = W + (intake − TDEE)/7700`, TDEE a random walk, scale readings as measurements; missing/incomplete intake = recent mean with a large σ (never 0), fasting = 0 kcal.
  4. **IMM** — two such filters (calm vs changing TDEE) with Markov switching.
  5. **RobustSlope** — Theil–Sen slope on the *raw* weigh-ins over a window, `TDEE = mean valid intake − slope·7700`, then EMA (no trend EMA, so no trend lag).
- **Split:** parameters chosen **only** on 120 calibration worlds (S01, S02, S04, S06, S09, S10 × seeds 1–20) by MAE; evaluated on 360 worlds (18 scenario variants S01–S17 incl. S12a/b × seeds 101–120; S18 has no data). 12 of those 18 (S03, S05, S07, S08, S11, S12a, S12b, S13–S17) were never used for calibration.
- **Metrics:** MAE and RMSE vs true TDEE (days ≥ 28); persistence forecast error `|estimate(d) − truth(d+h)|`, h = 7/14/28; recovery after a true step (half-step and within ±100 kcal for 7 days); false change = days with `|estimate(d) − estimate(d−14)| > δ` while the truth is constant; stability = mean |day-to-day change|; paired ΔMAE vs production with a 95 % bootstrap CI over worlds.
- **Honest process note:** the parameter grids were extended twice because optima sat on the grid edge (Kalman σT/σV/σI, IMM σT₂/stay, tuned window). Final residual edges: Kalman σI (flat plateau 250–900) and RobustSlope W 70 (not competitive anyway). Ties within 0.5 kcal for ProdTuned were broken toward the longer window (W 7/10/14 tie on calibration, 62.1–62.2, but generalise differently).
- **Calibration bias to know:** 4 of the 6 calibration scenarios have constant truth and 2 change, so the MAE objective favours stability.

## 2. Selected parameters (calibration only)
| estimator | selected | calibration MAE |
|---|---|---|
| Production | β 0.1, W 21 | 71.9 |
| ProdTuned | **β 0.035, W 14** | 62.2 |
| Kalman | σT 32 kcal/day, σV 1.2 kg, σI 900 kcal, σmiss 400 | 70.4 |
| IMM | σT 8 / 64, σV 2, stay 0.97, σI 900 | 70.1 |
| RobustSlope | W 70, β 0.2 | 77.9 |

## 3. Results on the evaluation worlds
**Accuracy — MAE / RMSE (kcal), mean over worlds**
| group | Production | ProdTuned | Kalman | IMM | RobustSlope |
|---|---|---|---|---|---|
| steady truth (S01, S13–S17) | 61.7 / 75.0 | **39.1 / 48.2** | 43.2 / 54.9 | 40.6 / 51.4 | 47.4 / 61.8 |
| confounded scale (water, glycogen, whoosh) | 131.4 / 158.4 | **75.7 / 90.9** | 85.7 / 103.9 | 80.1 / 95.7 | 85.0 / 103.0 |
| data gaps (S09, S10) | 65.5 / 79.2 | **44.6 / 52.5** | 50.6 / 62.4 | 48.5 / 59.3 | 52.1 / 65.3 |
| **truth changes (S02–S05, S11)** | **93.3 / 123.9** | 113.6 / 143.5 | 128.2 / 159.1 | 131.3 / 163.0 | 141.1 / 173.0 |
| start-up, TDEE₀ −12 % (S12a/b), days 28+ | 58.5 / 72.1 | 61.6 / 79.9 | 46.3 / 58.6 | 45.0 / 58.7 | **39.4 / 51.6** |
| **all 18 scenario variants** | 82.2 / 102.6 | **69.0 / 85.8** | 75.1 / 93.3 | 73.7 / 91.5 | 79.3 / 98.8 |
Paired ΔMAE vs production (negative = better): ProdTuned **−13.2** [−16.5, −9.9] (better in 68 % of worlds); Kalman −7.1 [−10.7, −3.5]; IMM −8.4 [−12.3, −4.5]; RobustSlope −2.9 [−7.0, +1.2] (not significant).

**Forecast error (kcal), persistence of the current estimate:** all scenarios h 7/14/28 — Production 88.9/96.1/113.3; ProdTuned **76.5/84.5/102.5**; Kalman 82.6/90.7/108.4; IMM 81.4/89.6/107.7; RobustSlope 87.3/95.9/113.7. When the truth is changing — Production **115.9/140.3/195.3**; ProdTuned 138.7/165.5/225.2; Kalman 153.4/180.5/239.1; IMM 156.9/184.2/243.7; RobustSlope 166.7/194.3/253.8.

**Recovery after a true step (days):** half-step — S04 17.1 / 25.3 / 31.9 / 35.5 / 42.6; S05 17.1 / 29.2 / 33.1 / 35.6 / 41.0; S11 (through Holding) 18.1 / 26.1 / 33.7 / 41.0 / 39.2 (order: Production, ProdTuned, Kalman, IMM, RobustSlope). Within ±100 kcal for 7 days — S04 26.7 / 51.2 / 55.3 / 56.7 / 59.4; S05 27.0 / 46.2 / 50.3 / 51.0 / 55.1.

**False change and stability (truth constant):** share of days with a ≥ 100 kcal move over 14 days — Production **34.2 %**, ProdTuned 12.7 %, Kalman 17.4 %, IMM 12.8 %, RobustSlope 11.6 %; ≥ 150 kcal — 17.1 / 4.8 / 7.3 / 5.4 / 3.8 %; mean |Δ|/day — 8.11 / 4.69 / 9.21 / 7.09 / 4.56 kcal.

**Initial vs learned, Holding:** during the first 28 days after a −12 % TDEE₀ the error is ≈ 226–266 kcal for all (nothing recovers the start-up faster than the data allow); after day 28 Kalman/IMM/RobustSlope are better (45–46 / 39 vs 58.5). MAE on days production is Holding (S09, S10, S11): Production 110.4, ProdTuned 85.3, Kalman 90.0, IMM 89.7, RobustSlope 95.5.

## 4. Findings
1. **No estimator dominates; there is a responsiveness ↔ stability frontier.** Production sits at the *responsive* end: best when the truth really changes (MAE 93; half-step in ≈ 17 days; best change-time forecasts) but the noisiest otherwise (steady 62, confounded 131; 34 % false-change days). Everything smoother is better in steady, confounded and gap scenarios and 8–25 days slower after a true step.
2. **A tuned production structure beats every prototype overall.** ProdTuned (β 0.035, W 14, same functions and gating) has the lowest overall MAE/RMSE and the best forecasts across all scenarios; Kalman and IMM do not beat it (75.1 / 73.7 vs 69.0) while being far more complex and needing four noise parameters.
3. **Kalman/IMM are only better at start-up (days 28+).** Their calibrated noise parameters drift to "trust the scale and the intake less" (σV 1.2–2, σI 900), i.e. the white-noise model does not match autocorrelated water noise; IMM's extra mode gives little.
4. **Water/glycogen/whoosh is the biggest error source for production** (131 kcal MAE vs 76–86 for every smoother alternative).
5. **Raw-weight robust slope is not competitive** overall (79.3, ΔMAE not significant), though best at start-up.
6. Nothing fixes the first four weeks after a wrong TDEE₀ — information-limited, not method-limited.
7. Floors: even the best estimator has MAE ≈ 39 kcal in steady state; uncertainty of the learned TDEE is large relative to the 100–400 kcal changes users care about.

## 5. Recommendation (no production change is made or proposed for activation)
- **Do not adopt Kalman or IMM**: not better than the cheaper tuned production structure, more complex, weaker at responding to real change.
- **If the product prefers stability** (fewer false changes, lower overall error, better Holding behaviour), the lowest-risk candidate is the **same estimator with β ≈ 0.035–0.05 and W ≈ 14**; expected effect on this simulator: overall MAE −13 kcal (−16 %), false-change days 34 % → 13 %, at the cost of **≈ +8–12 days to cover half of a true step and +20 kcal MAE when expenditure truly changes**. This is a **product trade-off decision** (and would need a version bump, a rebuild decision and a G0/label review); it is *not* recommended to apply without that decision. Alternative not studied (bounded round): an adaptive β that is fast only when a change is supported by the data.
- Keep the weight trend (N = 20), the window rules and the G0 machine as they are.

## 6. Flux re-assessment — is there now a mathematical basis? **No. Keep Flux hidden.**
Target (MF-OFFICIAL, help 74): an "indicator of change magnitude" and the "navigable space" of trajectories the algorithm "could have taken". What the evidence shows:
| Test | Result |
|---|---|
| Envelope of responsiveness variants (5 families, Report E) | width does **not** rank-track the true change magnitude (held-out ρ ≤ 0.07; ≤ 0.19 with 28–42-day windows; ≈ 0.40 even without noise) and over-expands on confounds |
| The one principled trajectory-uncertainty object — **Kalman posterior sd of the TDEE** | ≈ **200 kcal** (201 on days the truth is stable vs 196 when it is changing); Spearman vs true change 0.073 / −0.012 / −0.084 (14/28/42-day windows): it measures how little the data can say, not whether expenditure moved |
| Identifiability of that model's noise parameters | partly: 9 of 80 settings within 1 kcal of the best, and the MAE ranges 70.4–118.9 over the grid, but σI is flat (250–900 equal) so any band width would be set by an arbitrary choice |
| Estimator itself | MAE ≥ 39 kcal in steady state; a stability/responsiveness trade-off that the user has not yet decided (§5) — a Flux defined on the current learned path would move if that is changed |
**Evidence still missing before a meaningful Flux can be built:** (1) an operational definition of MF's "change magnitude" / "navigable trajectories" (MF has not published the formula, the width drivers, or Holding/Calibrating/Static behaviour); (2) a few MF reference traces *with their input data* (weights, intake, status) so a width law can be inferred and tested, instead of fitting a screenshot; (3) real-user noise statistics (log error, water autocorrelation, weigh-in frequency) to replace the simulator's assumptions; (4) the user's choice for the estimator balance (§5), because Flux is defined relative to it; (5) the product requirement: a change indicator, or an uncertainty/data-quality band (which MF says Flux is not). Until then no code beyond the unwired research engine, and the API returns null.

## 7. Open business-verification questions (single list for the user to close)
| # | Area | Question | Where | Status / default |
|---|---|---|---|---|
| 1 | Estimator | ~~Prefer responsiveness or stability?~~ **CLOSED: production kept as is (user).** Version B (β 0.05, W 14) is a flagged-off research proposal (§9); activating it would need the §9 safety conditions, a roll-forward rollback and a rebuild decision | §5, §9 | closed; B parked |
| 2 | Flux | Keep hidden (decided). When revisited: Holding frozen band? hide after N days? Static? D/W/M aggregation? | Report B §8, C §3 | parked until §6 evidence exists |
| 3 | Goal Tracking | Q1 no Scale/Trend picker anywhere? Q2 who audits MuscleGain/Endurance/Flexibility (Endurance/Flexibility direction is "pending user decision" in `GoalRules.cs:19-20,35-36`)? Q3 what is a *valid measurement*? Q4 "reached" = latest or any valid weigh-in? Q5 can the reached prompt be dismissed? Q6 if the reaching weigh-in is edited/deleted? Q7 trend-reached completion? | `Goal_Tracking_Mode_Proposal.md` §13.11 | UNKNOWN (no MF reference); S28 not approved |
| 4 | Goal Tracking | Q8 New Goal/Program while the prompt is unanswered? Q9 what Goal Progress shows after Complete? Q10 Strategy Goal Card + Edit Goal after Complete? Q11 target edit while reached (block vs re-evaluate)? Q12 MF screenshots of the reached/complete flow? Q13 achievement storage O1–O4? Q14 keep `StartWeightKg` as the Trend start + new raw start column? Q15 goal-only creation vs coupled with Program? | same | UNKNOWN |
| 5 | D8 | Approve fix F1 + F2 (stop `EnsureActiveGoalAsync` creating goals; make the two GETs read-only), F3 (data backfill migration), F4 (unique active goal index)? | D8 report | recommendation F1 + F2 now, F4 with S28; not applied |
| 6 | Macros | Draw a negative **Other**: not drawn (current FE) + note, or another form? MF drawing is UNKNOWN; percentages stay un-normalised | Macros audit | current behaviour kept; MF unverified |
| 7 | Macros | MF denominator for displayed **averages** (logged days only vs calendar, fasting/incomplete/missing) | Macros audit | UNKNOWN; current = per logged day, fasting counts, incomplete/missing excluded |
| 8 | Weights | Scale vs SmartScale on the same day: two methods or merged? | earlier reports | open |
| 9 | Steps | Steps average divisor | earlier reports | open |
| 10 | Adherence | Display of legacy (`isUnverified`) days in adherence | earlier reports | open |
| 11 | Expenditure | "Learn from day 1" flag (`FreezeTdeeDuringCalibration = false`) release approval | `AlgorithmOptions.cs:31-37` | pending approval |
| 12 | Expenditure | Label hysteresis `SmoothLabels` (2-day rule) approval | `ExpenditureSeries.cs:26-34` | pending approval, off |
| 13 | Check-in | ON_TARGET tolerance policy (δ, k, λ; classifier off) | `CheckIn_OnTarget_Tolerance_Proposal.md`, `AlgorithmOptions.cs:50-60` | proposal pending; inactive |
| 14 | Onboarding | Prior Weight threshold (weight ÷ 0.9) is MACROOS_PROVISIONAL_POLICY, not MF-verified | `PriorWeightQuestion.cs` | open |
| 15 | Data hygiene | Latest-weight lookups ignore recency (stale trend feeds Start/targets) and fall back to raw weight when trend is null; start snapshots are not refreshed after history edits | earlier Start audit | observation; no decision yet |
Closed by the user in this series: Program Initial Weight = Trend; Flux kept hidden; S27 applied on production. The Goal Tracking defaults D1–D13 were *not* adopted (answers pending as items 3–4).

## 8. Limits
Synthetic only; one noise model; calibration objective MAE with a stability-leaning scenario mix; grids chosen by me and extended twice; 20 seeds per scenario; no real data; "better" here means closer to the simulated truth, which says nothing about user-perceived behaviour.

## 9. Closing (user decision: "Giữ production như hiện tại" — production is NOT changed; this closes the estimator research)
Scope of this section: store the β 0.035–0.05 / 14-day-window result, compare it on **fresh seeds** (101–120) split into strictly held-out scenarios and calibration scenarios, apply an acceptance rule **fixed before looking at the numbers**, write Version B as a flagged-off proposal, and stop. Full output: `F_Closing_Evaluation_Output.md` (`dotnet run --project tests/FluxResearch -c Release -- closing`). No parameter search was repeated.

**Acceptance rule (declared first):** a candidate beats the baseline only if (1) overall MAE is lower with a paired-bootstrap CI excluding 0, (2) false-change days drop, (3) half-step response ≤ 1.25 × production, (4) MAE when the true TDEE changes ≤ 1.15 × production (so lower MAE never wins if the response to real change is much worse).

| candidate (W = 14) | ΔMAE overall [95 % CI] | false change ≥ 100 kcal | half-step | MAE when truth changes | verdict |
|---|---|---|---|---|---|
| β 0.07 | −3.7 [−4.4, −3.1] | 33.9 % vs 34.2 % | ×0.98 | ×0.98 | beats baseline (small gain, no cost) |
| **β 0.05** | **−10.7 [−12.5, −9.0]** | **22.3 % vs 34.2 %** | **×1.18** (20.5 vs 17.4 d) | ×1.05 | **beats baseline** |
| β 0.035 | −13.2 [−16.6, −10.0] | 12.7 % vs 34.2 % | ×1.54 | ×1.22 | **does not** (too slow after real changes) |
**Per situation, β 0.05 / W 14 vs production (MAE kcal; held-out scenarios unless stated):** rapid rise (S11) 72.7 vs 79.9; stable truth (S13–S17) 48.1 vs 62.9; water/glycogen/whoosh (S07, S08) 115.7 vs 157.1; all held-out scenarios 73.1 vs 85.0; rapid fall (S03 ramp, S05 step) **109.3 vs 97.0 (worse, +13 %)**; start-up (S12a/b) 57.0 vs 58.5; fresh-seed calibration scenarios: rapid rise (S02, S04) 99.6 vs 96.3 (worse, +3 %), water (S06) 62.1 vs 80.1, missing logs / weigh-in gap (S09, S10) 51.8 vs 65.5. **Not covered by rule (3): recovery to within ±100 kcal of the new truth is 40–47 % slower** (S04 26.7 → 39.3 d; S05 27.0 → 37.9 d; S11 18.1 → 22.5 d). Whether that counts as "significantly slower" is a product judgement left to the user.

**Can stability improve without slowing the response too much? Partly.** β 0.07 / W 14 is almost free but small. β 0.05 / W 14 removes about a third of the false changes and 13 % of the error, for ≈ +3 days to half-step and ≈ +10–12 days to settle within ±100 kcal after a real step. Going further (β 0.035) buys more stability but fails the rule. A method that is fast only when the data support a change (adaptive) might beat this frontier; it was not studied (bounded round) and is not claimed.

### The five conclusions requested
1. **Limitations of the production estimator (measured):** noisiest part of the frontier — 34 % of constant-truth days show a ≥ 100 kcal 14-day move; MAE 131 kcal when scale readings are confounded by water/glycogen; 110 kcal on Holding days; start-up after a wrong TDEE₀ is information-limited (≈ 237 kcal for 28 days, no estimator does better). Responsive (half-step 17 days) is its strength.
2. **What Version B improves:** fewer false changes, lower error in steady, confounded and missing-data situations, better 7/14/28-day forecasts overall.
3. **Real trade-offs:** slower recovery after a real change (≈ +40 % to reach ±100 kcal; +18 % to half-step), +13 % error on rapid falls, +3 % on rapid rises. The same trade-off will be felt by users as slower diet adjustments after a true metabolic change.
4. **Conditions to be safe to replace production:** (a) the user explicitly prefers stability over responsiveness; (b) a read-only **shadow replay** on real (consented) history comparing A and B, with no stored output, showing the same direction as the simulator; (c) MF-V3 behaviours above not contradicted; (d) a flag with per-user or global switch and a tested roll-forward rollback; (e) AlgorithmChecks vectors for B and a rebuild policy decision (no automatic historical rebuild); (f) monitoring of the share of users with large 14-day TDEE moves and check-in volatility after release.
5. **Missing evidence:** real-user noise (the simulator's water AR(1), 7 % log error and weigh-in frequency are assumptions); any ground truth for real TDEE; MF's V3 method and parameters (not published); how users perceive slower updates; an adaptive-β design (not studied).

### What Expenditure V3 actually publishes (MF-OFFICIAL: macrofactor.com/expenditure-v3, help.macrofactorapp.com articles 74 and 20; relayed by a fetch summariser, re-check before quoting)
| MF statement | Relation to this study |
|---|---|
| "day-to-day expenditure updates are generally about 35% smaller" than V2; "reduced over-correction", "earlier response to true changes", "better handling of missing data", "~10% improvement in prospective accuracy" | MF claims **both** more stability **and** earlier response — i.e. a method that moves the frontier, not a re-tune. A smaller β only slides along our frontier (steadier **and slower**). So nothing here reproduces V3. |
| "you can't simultaneously maximize both stability and responsiveness" (signal and noise discussion) | consistent with the trade-off found here |
| updates pause only with more than three missing-nutrition days in seven; minimum nutrition 4 of 7 days and weight once a week; recommended daily nutrition and weight ≥ 3×/week | matches the G0 4/7 rule and 1 weigh-in/7 d already in `ExpenditureStatusMachine`/`ExpenditureCalculator`; unchanged by B |
| energy intake estimates "generally within about 15-20%" of actual; partial logging is the main failure | the simulator uses 7 % daily log noise (optimistic vs MF's 15–20 %) |
| the technique is withheld ("secret sauce"); no formula, β or window is published | **β 0.05 / window 14 must not be described as MF's formula** |

### Version B — research proposal (NOT activated; no code written)
- **Definition:** same pipeline as A (trend N 20, G0 machine, `ComputeRaw`, EMA) with `Algorithm:SmoothingBeta = 0.05` and `Algorithm:ExpenditureWindowDays = 14` (both options already exist and are read at `ExpenditureRebuilder.cs:26,132`; no formula change). Nothing else moves.
- **Switch (flagged off):** a single config value (e.g. `Algorithm:ExpenditureEstimatorVersion`, default A) read where `AlgorithmVersions.ExpenditureFor(opt)` already chooses the stamp; B gets a new label (proposed `"8"`, with the Calibrating-freeze variant handled as today).
- **Rollback — important:** versions are monotonic. `ExpenditureRebuilder.cs:77-83` **skips any day whose snapshot was written by a newer version** (`AlgorithmVersions.IsNewer`). Switching the flag back to A therefore would **not** overwrite B's stored days. Rollback must be **roll-forward**: release a new label (e.g. `"9"`) whose parameters equal A and let normal rebuilds replace B's rows, or explicitly rebuild from the activation date. Without a rebuild the series keeps B's values until each day is recomputed.
- **Continuity:** TDEE is carried from the last stored snapshot, so enabling B creates no jump; only the speed of later updates changes. No historical rebuild is part of the proposal (history stays A until someone approves a rebuild).
- **Calculation revisions:** `UserExpenditureSettings.CalculationRevision` is documented as "bumps when the Start Date changes" (`UserExpenditureSettings.cs:14`), but **no code writes it today** (read-only at `ExpenditureSettingsSupport.cs:56`; `ExpenditureSnapshot.CalculationRevision` is never stamped; Reset/SetManual only move `program.ExpenditureStartDate`) — corrected 2026-10-23, see `Docs/Design/Ops/Mutation_Dependency_Matrix.md` §3. An algorithm change would not bump it either. B is distinguished by `ExpenditureSnapshot.AlgorithmVersion` (and `RecalculationJob.AlgorithmVersion`); revisions stay as they are. Static mode (overlay, not wired) and the Goal/Program/Check-in logic are untouched, but check-in proposals read the stored TDEE, so they will change more slowly under B.
- **Out of scope until approved:** shadow replay on real data, any rebuild, any migration, any production switch.

**Verdict: the estimator research is closed.** Production stays as is (user decision). One candidate (β 0.05, W 14; and the near-free β 0.07, W 14) beat the baseline on the agreed criteria, so Version B is documented above as a proposal; no further rounds. Remaining business-verification items are in §7.
