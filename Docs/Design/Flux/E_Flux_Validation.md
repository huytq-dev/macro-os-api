# Report E — Flux synthetic validation (Phase 6)

Date 2026-10-23. Reproduce: `dotnet run --project tests/FluxResearch -c Release -- all` from `BE/` (about 10 s, no DB, no API). Full machine output: `E_Research_Run_Output.md`; day-by-day data of one seed per scenario: `E_data/<id>.csv` (true TDEE, learned TDEE + status, the four candidate trajectories, Lower/Upper, state). **Flux width is NOT evaluated as confidence-interval coverage** (per the brief); "truth inside the band" appears only as a diagnostic and no pass/fail depends on it.

## 1. Method and assumptions (MACROOS assumptions, not MF's)
- **World:** known true TDEE per day; true weight moves by (intake − TDEE)/7700 (symmetric kcal/kg as in `AlgorithmOptions.KcalPerKg`); start weight 85 kg; planned deficit 300 kcal (500 in S08, 0 after day 60 in S14); daily adherence noise 4 %; logged kcal = actual × (1 + 7 % noise); valid log 93 % of days; weigh-in 90 % of days; scale = true weight + water AR(1) (φ 0.6, σ 0.35 kg → ≈ 0.44 kg stationary) + scenario shifts + 0.1 kg measurement noise; TDEE₀ = truth × (1 + 6 %) (S12: −12 %).
- **Production estimator replicated with the production functions** (`ProductionReplica` in `tests/FluxResearch/World.cs`): `WeightTrendCalculator` (N 20), `ExpenditureStatusMachine.Next`, `ExpenditureCalculator.ComputeRaw` (W 21) / `Smooth` (β 0.1), 14-day Calibrating freeze (S12b: learn from day 1). The replica mirrors `ExpenditureRebuilder.cs:121-132` but is **not** the rebuilder (no DB).
- **Splits:** calibration scenarios S01, S02, S04, S06, S09, S10 (seeds 1–20); everything else is held-out; validation seeds 101–120 (380 worlds). 18 requested scenarios ↔ ids: 1 S01 · 2 S02 · 3 S03 · 4 S04 · 5 S05 · 6 S06 · 7 S07 · 8 S08 · 9 S09 · 10 S10 · 11 S11 · 12 S12a/S12b · 13 S13 · 14 S14 · 15 S15 · 16 S16 · 17 S17 · 18 S18.
- **Metrics:** learned MAE (days ≥ 28, not Calibrating); width = Upper − Lower; "stable" days = true change over the last 14 days ≤ 50 kcal, "change" days ≥ 150; τ = 1.5 × median stable width on S01 (calibration seeds, per band set; held fixed for validation); **false expansion** = share of stable days with width > τ; **onset lag** = days from the true event to width > τ; **recovery** = days from the event until width ≤ τ for 7 consecutive days; **flicker** = mean |Δ Lower| + |Δ Upper| per stable day; objective **Spearman ρ(width, true change magnitude)**.

## 2. Per-scenario results — C3 (the V1 candidate), mean of 20 held-out seeds
| scenario (ground truth) | split | learned MAE | width mean | width stable | width change | false exp. | onset (d) | recovery (d) | flicker | truth in band % (diag.) |
|---|---|---|---|---|---|---|---|---|---|---|
| S01 stable 2500 | cal. fresh seeds | 56 | 81 | 81 | – | 33 % | – | – | 11 | 49 |
| S02 ramp 2400→2800 (d30–90) | cal. fresh | 89 | 111 | 99 | – | 45 % | 13.7 | 46.6 | 13 | 28 |
| S03 ramp 2800→2300 | held-out | 91 | 116 | 108 | – | 45 % | 12.8 | 44.5 | 14 | 31 |
| S04 step 2400→2800 (d60) | cal. fresh | 104 | 119 | 121 | 104 | 52 % | 6.5 | 46.2 | 13 | 35 |
| S05 step 2800→2400 (d60) | held-out | 103 | 119 | 122 | 94 | 56 % | 5.5 | 41.0 (19/20) | 13 | 37 |
| S06 water +1.8 kg d50 (half-life 6 d) | cal. fresh | 80 | 103 | 103 | – | 45 % | – | – | 15 | 47 |
| S07 glycogen ±1.2 kg ×2 | held-out | 148 | 184 | 184 | – | 82 % | – | – | 23 | 45 |
| S08 plateau + whoosh (deficit 500) | held-out | 166 | 181 | 181 | – | 70 % | – | – | 22 | 35 |
| S09 missing logs (35 % + 7-day gap) | cal. fresh | 63 | 89 | 89 | – | 35 % | – | – | 11 | 47 |
| S10 no weigh-ins 25 d | cal. fresh | 68 | 95 | 95 | – | 38 % | – | – | 10 | 43 |
| S11 Holding→Updating (13 d no logs, +200 step d55) | held-out | 80 | 97 | 96 | 102 | 41 % | 8.2 | 40.5 | 12 | 35 |
| S12a Calibrating→Updating (frozen 14 d, TDEE₀ −12 %) | held-out | 54 | 76 | 76 | – | 23 % | – | – | 11 | 46 |
| S12b learn from day 1 | held-out | 64 | 81 | 81 | – | 33 % | – | – | 12 | 39 |
| S13 Static (data = S01) | held-out | 56 | 81 | 81 | – | 33 % | – | – | 11 | 49 |
| S14 goal change (deficit 300→0 d60) | held-out | 70 | 93 | 93 | – | 38 % | – | – | 12 | 48 |
| S15 program change, no TDEE reset (data = S01) | held-out | 56 | 81 | 81 | – | 33 % | – | – | 11 | 49 |
| S16 backdated log (d40–46 entered late) | held-out | 60 | 85 | 85 | – | 34 % | – | – | 12 | 48 |
| S17 weekly weigh-ins | held-out | 72 | 98 | 98 | – | 43 % | – | – | 17 | 45 |
| S18 no valid observations | held-out | – | no band | – | – | – | – | – | – | – |
(Other candidate sets — L0 legacy, C1, C2, C4, C5 — and exact decimals are in `E_Research_Run_Output.md`; each scenario's Lower/Upper per day are in `E_data`.) Learned-estimator responsiveness: days to cover half of the true change — step ≈ 17 (S04, S05), Holding→Updating step ≈ 18, 60-day ramps ≈ 46–49.

## 3. Does any band indicate change magnitude? (the key question)
| set | held-out ρ (14-d truth window) | 28 d | 42 d | fresh-seed ρ (calibration scenarios) | held-out width: stable truth / truth changing |
|---|---|---|---|---|---|
| L0 legacy ±37 | undefined (constant 74; also drawn in S18 with no observations; truth inside only 14–45 % of days per scenario) | – | – | – | 74 / 74 |
| C1 β-envelope | 0.056 | 0.135 | 0.139 | 0.060 | 38 / 32 |
| C2 W-divergence | 0.062 | 0.150 | 0.187 | 0.121 | 101 / 94 |
| **C3 combined (V1)** | **0.066** | **0.154** | **0.188** | **0.113** | **107 / 98** |
| C4 motion (range of learned TDEE) | 0.010 | 0.047 | 0.089 | 0.058 | 187 / 126 |
| C5 net displacement | 0.030 | 0.081 | 0.132 | 0.070 | 144 / 82 |
**Reading:** every family's width is about as large when the true expenditure is stable as when it is changing; ρ stays below 0.2 even if "change" is windowed over 42 days. Noise sweep (calibration scenarios, seeds 501–520, parameters frozen): C3 ρ = 0.40 at zero noise, 0.39 / 0.37 / 0.29 at 0.25× / 0.5× / 1× noise — i.e. even without noise the relation is only moderate (the band lags a change and stays wide for ≈ 40–60 days), and realistic noise erodes it further. **Conclusion: with the production estimator and this noise model, none of these bands is a validated "change magnitude indicator"; they mostly report estimator disagreement caused by noise and data confounds.** This depends on the simulator's noise model (Report B Q7).

## 4. Invariants and rules — all PASS (380 validation worlds, C3)
Lower ≤ learned TDEE ≤ Upper every day; deterministic; **Calibrating ⇒ no band** (S12a: 14 frozen days; S12b: 2 days); **no valid observations ⇒ null** (S18); **Holding ⇒ band unchanged** (S11: 11 consecutive Holding days); **causality** (band at day d unchanged when data after d is removed, S04); **Static** (S13) and **program change without TDEE reset** (S15) identical to S01; **backdated log** (S16): days before 40 unchanged, 78 later days recomputed; goal change (S14) does not blow up the band. Unit tests in `AlgorithmChecks` (844 PASS): hand-computed trajectory vector (2500 → 2635 → 2702.5 → 2736.25), containment, Calibrating/Holding/NoObservations, causality + determinism, data-dependent width ≠ legacy 74, observation resolver (null vs 0 vs fasting).

## 5. Limits of this validation
Synthetic only (assumptions in §1); 5 grid values per axis; plausibility screen non-binding; ρ vs a 14-day truth window is a chosen criterion (sensitivity shown); no real-user data and no MF ground truth were used or available; results do **not** say the V1 band equals MF's Flux.
