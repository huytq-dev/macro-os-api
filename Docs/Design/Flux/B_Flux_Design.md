# Report B — Flux design (Phases 2–3): calculator, policy, candidates, rules, calibration

Date 2026-10-23. **Status: design + synthetic validation only. Nothing is wired into any handler, API, DB, FE or production behaviour. No migration. Nothing approved.** Policy name `MACROOS_FLUX_V1` is a **MACROOS POLICY**; it is **not** claimed to equal MacroFactor Expenditure V3 (MF has not published the formula). Evidence tags as in Report A (**CODE / MF-OFFICIAL / OBSERVED / UNKNOWN**) plus **POLICY** (a choice made here, needs approval).

## 0. The answer in five lines
1. A pure, causal, deterministic `ExpenditureFluxCalculator` + versioned `FluxCalculationPolicy` + a shared `ExpenditureObservationResolver` now exist in `src/Application/Features/Expenditure/Flux/` (not referenced by any handler).
2. The band is the min/max **envelope of trajectories of the same estimator structure as production** (windowed energy balance → EMA) with different responsiveness, **plus the learned TDEE**, so `Lower ≤ TDEE ≤ Upper` by construction; no constant ± kcal and no fixed %.
3. Parameters were chosen **only** on calibration scenarios (S01, S02, S04, S06, S09, S10 × 20 seeds) and checked on 13 held-out scenarios + fresh seeds: slow β 0.07, fast β 0.14, short W 14, long W 56.
4. **Result of the validation is negative on the objective that matters:** none of the five candidate families rank-tracks the *true* change magnitude (held-out Spearman ρ ≈ 0.01–0.07; ≈ 0.15–0.19 if "change" is windowed over 28–42 days), and the bands widen a lot when the truth is stable but the data are confounded (glycogen/plateau/sparse weigh-ins). See Report E.
5. **Recommendation: do not activate.** Keep Flux hidden (the FE already hides the old one) until the user decides which of the options in §8 to pursue.

## 1. What MF says (and does not)
- **MF-OFFICIAL** (help article 74, relayed by a page-summarising fetch tool): "Flux range is an indicator of change magnitude"; it marks periods where "notable movement/change in your expenditure is occurring"; the band is the "navigable space" — "the algorithm could have taken any path through that shaded area"; it is *not* "technically" a confidence interval; "a larger flux range represents a larger potential for error, but this is not a direct relationship"; the algorithm "behaves the same" with a larger range; shown "for fun and curiosity"; V3 can "navigate through the flux range intelligently instead of relying on a fixed route".
- **OBSERVED** (`Dashboard/Expenditure/1-chart-collapsed.png`, `Expenditure_v2/01_mf_chart_flux_range.png`): the band is drawn behind the line **while Holding**; ≈ ±15 kcal in one early-stage account; the edges taper near the last points.
- **UNKNOWN (not stated anywhere found):** what drives the width; whether it is drawn in Calibrating or in Static; how D/W/M averaging aggregates it; any numbers.

## 2. Architecture
```
FoodLogs ─┐                         ┌─► ExpenditureFluxCalculator.Trajectory(spec)  ×K candidates  ┐
          ├─► ExpenditureObservation│                                                               ├─► Band(): floor(min) / ceil(max) of {candidates, learned TDEE}
WeightTrend┘     Resolver (shared)  └─ learned snapshots (TDEE + status per day) — INPUT only ─────┘
```
| Piece | File | Contract |
|---|---|---|
| Observations | `ExpenditureObservationResolver.cs` | exactly the engine's valid observations (`ExpenditureRebuilder.cs:35-41`): `KcalIn` = `LoggedDay.AlgorithmKcal` (unlogged / incomplete / 0-kcal-not-fasting = **null, never 0**; fasting = 0), `TrendKg` **only on weigh-in days** (interpolated days are not independent measurements). Meant to be shared by the rebuilder later; the rebuilder is **not** changed now. |
| Candidate | `FluxCandidateSpec(Name, Beta, WindowDays)` | same structure as production: `ExpenditureCalculator.ComputeRaw` on the last W days, then `ExpenditureCalculator.Smooth` with β — **the same production functions, unmodified** (`ExpenditureFluxCalculator.cs`). Trend weight is the stored production trend (not recomputed). |
| Policy | `FluxCalculationPolicy(Version, Candidates)`, `V1` | versioned; V1 = `slow(0.07,21) fast(0.14,21) short(0.10,14) long(0.10,56)`; production nominal = (0.10, 21). Every change ⇒ new version string. |
| Calculator | `ExpenditureFluxCalculator.Compute / Trajectory / Band` | pure, deterministic, **causal** (day d uses observations ≤ d only). Input: observations + the learned series (TDEE + status per day, the stored snapshot series). Output: `FluxPoint(Date, LowerKcal?, UpperKcal?, State, CandidateKcal)`. Writes nothing; nothing reads it back into TDEE, targets, check-ins, Calories/Macros/Strategy. |
| Why the learned series is an *input* | the stored snapshots may predate the current code, or come from an older algorithm version; including the stored value in the envelope guarantees containment regardless |

Why "same structure as production": CODE shows the production smoother is **not** a single EMA on the weights — it is a 21-day windowed energy balance (`ExpenditureCalculator.cs:12-29`) followed by an EMA β = 0.1 on the TDEE (`:44-45`, `ExpenditureRebuilder.cs:132`), on top of a 20-day EMA weight trend. Candidates therefore vary exactly those two knobs.

## 3. Candidate families evaluated
| ID | Definition | Why considered |
|---|---|---|
| L0 | legacy `±round(0.1·7700/max(WindowDays,7))` | baseline; an assumed-error band, not data-driven |
| C1 | envelope of {slow β, production, fast β} at W = 21 | "multi-trajectory envelope", responsiveness axis |
| C2 | envelope of {short W, production, long W} at β = 0.1 | "short-term vs long-term divergence" |
| C3 | envelope of C1 ∪ C2 (4 candidates + production + learned) | both axes — **the V1 candidate** |
| C4 | range of the learned TDEE over the last H days | better-founded alternative: directly "how much did the estimate move" |
| C5 | \|TDEE(t) − TDEE(t−H)\| (net displacement) | same idea, less noise-accumulating than a range |
Rejected without running: a bootstrap/propagated-error band — it measures noise, i.e. exactly the "uncertainty band mislabelled Flux" the audit found.

## 4. Rules (all enforced by tests, see Report E §4 and `AlgorithmChecks`)
| Situation | Rule | Basis |
|---|---|---|
| **Calibrating** | `null` band (no draw) | user rule; production Calibrating = frozen/initial TDEE |
| **No valid observation ever** (never Updating, or no data at all) | `null`, state `NoObservations` | no information |
| **Updating** | band = envelope of all candidates and the learned TDEE | — |
| **Holding** | **drawn, frozen at the last band**: candidates do not update because the engine received no new valid observations; the learned TDEE is carried | POLICY justified by: no new information ⇒ no new plausible paths; MF draws a band in Holding (**OBSERVED**, one account). A "hide after N Holding days" rule is **UNKNOWN** — not invented; the FE can derive Holding age from `status`. |
| **Static mode** | Flux is computed **only** from the learned estimator and never changes the static value; the calculator has no static input (test S13: identical input ⇒ identical band) | Static overlay is not wired and never rewrites learned snapshots (`ActiveExpenditureResolver.cs:4-6`). **Whether to draw it while Static is active: UNKNOWN** (no MF evidence). POLICY proposal: hide while Static is the displayed line (a learned-path band around a user-set line would mislead). Needs the user's decision. |
| Missing nutrition | null, never 0 | resolver |
| Incomplete day | not a valid day (null) | `AlgorithmKcal` |
| Fasting | 0 kcal valid day, per the approved G0/fasting rules | `FoodLogSupport.cs:45` |
| Interpolated weights | not observations; only weigh-in days carry `TrendKg` | resolver |
| Backdated / edited history | a derived rebuild recomputes from the changed day; earlier days unchanged (test S16) | same as the learned series |
| Goal / program change | not inputs ⇒ no effect (tests S14/S15) | engine has no goal/program input |
| Rounding | `Lower = floor`, `Upper = ceil` so the rounded TDEE is always inside | — |

## 5. Parameters and how they were chosen (no screenshot fitting)
- **Calibration set:** S01 stable, S02 ramp up, S04 step up, S06 water, S09 missing logs, S10 weigh-in gap — seeds 1–20. **Held-out:** S03, S05, S07, S08, S11, S12a/b, S13–S18 plus the calibration scenarios again on fresh seeds 101–120. The held-out scenarios and fresh seeds were never used to choose anything.
- **Plausibility screen (POLICY, κ = 1.5):** a candidate trajectory counts as "a path the algorithm could have taken" only if an estimator with those settings would itself be acceptable (MAE ≤ κ × production MAE on the calibration set). In this grid **every** candidate passed (largest ratio 1.18), so κ did not constrain the choice; the grid edges (β 0.025–0.3, W 7–56) are therefore the real limits and the choice is sensitive to that grid.
- **Objective:** pooled Spearman ρ between band width and the **true** change magnitude (max − min of the true TDEE over the trailing 14 days). Scale-free, so it cannot be tuned to make the chart look like a screenshot; ties within 0.005 go to the narrower band.
- **Selected (C3):** β 0.07 / 0.14, W 14 / 56, calibration ρ 0.152. C1 alone: β 0.07/0.14 (ρ 0.102); C2 alone: W 14/56 (ρ 0.155); C4: H 28 (ρ 0.074); C5: H 28 (ρ 0.101).
- Sensitivity: κ 1.25 / 1.5 / 2 select the same grid (non-binding); the truth-window used to define "change" (14 / 28 / 42 d) changes held-out ρ from ≈ 0.07 to ≈ 0.19 (Report E §3).

## 6. Findings that bear on the decision (all from the synthetic runs; assumptions listed in Report E §1)
1. **No candidate indicates true change magnitude reliably.** Held-out pooled ρ (14-day truth window): C1 0.056, C2 0.062, C3 0.066, C4 0.010, C5 0.030; legacy: undefined (constant). Even with **zero** simulated noise the best ρ is only ≈ 0.40 (C2/C3), falling to ≈ 0.29 at the default noise level on the calibration scenarios — so part of the limit is structural (the band lags and persists long after a change) and part is noise (Report E §3).
2. **False expansion is large:** with τ = 1.5 × the calm width, 33 % of stable-truth days in S01 are "expanded"; 70–82 % in glycogen/plateau scenarios; 43 % with weekly weigh-ins. The band mostly reflects *data confounds and estimator noise*, which is closer to MF's "potential for error" phrase than to "change magnitude".
3. **Production estimator facts** (not changed): pooled MAE 72 kcal on the calibration set; 53–166 kcal across scenarios; a true +400 step is half-covered after ≈ 17 days, a 60-day ramp after ≈ 46–49 days; and **slower smoothing (β 0.035–0.05) has *lower* MAE (0.89–0.90 ×) than β 0.1** in these conditions. Reported only; the brief forbids changing the learned estimator for Flux.
4. The old `±37` band is constant and uninformative as an indicator (width 74 always; truth inside the band only 14–45 % of days) — it is also not claimed as a CI. It is also **drawn when there is nothing to estimate**: in S18 (no valid log, no weigh-in) the legacy rule still yields a ±37 band around the carried TDEE (it depends only on `WindowDays`), whereas the new rules return null.

## 7. What was NOT done (per the brief)
No change to the learned TDEE, G0 machine, rebuilder, Calories/Macros/Strategy/Check-in; no historical rebuild; no S25/S26/D2 or any migration; no shared/production DB access; no Kalman/IMM; nothing committed/pushed/deployed; no claim that V1 equals MF V3.

## 8. Options and decisions needed (UNKNOWN items are questions, not assumptions)
| Option | What it means | Impact |
|---|---|---|
| **O1 (recommended): keep Flux hidden**; leave BE/FE as is; continue only if a clear product definition appears | no schema, no FE work; the old `fluxLowKcal/High` stays computed-but-hidden or is removed later | zero risk |
| O2: activate C3 as a **MacroOS "estimate-disagreement band"** (explicitly *not* a change indicator, not MF) | needs Report C's storage + contract; FE draws a band whose width mostly reflects data quality and noise | passes all structural rules; fails the change-magnitude objective; high false expansion |
| O3: first revisit the **estimator** (e.g. slower β / better window), then redo Flux | outside this round's scope; production behaviour would change; needs its own approval and a rebuild decision | could make any Flux more informative |
Questions for the user: **Q1** Which option? **Q2** Is "indicator of change magnitude" (MF wording) the product requirement, or is a data-quality/uncertainty band acceptable (then it should not be called Flux)? **Q3** Draw Flux while Holding (proposal: yes, frozen)? Hide after N Holding days (UNKNOWN)? **Q4** Draw Flux while Static (proposal: no)? **Q5** D/W/M aggregation of the band (Report C §3)? **Q6** Accept κ = 1.5 and the 14-day truth window as the calibration criteria, or define others? **Q7** Is the simulator's noise model (Report E §1) acceptable as the calibration world?
