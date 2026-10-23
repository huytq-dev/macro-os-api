# Report A — Expenditure Flux audit (Phase 1)

> **Update (same day, after the user's "giữ ẩn Flux" decision):** the legacy band described in §2–§3 was **removed from the API output** — `GET /api/expenditure/series` and `GET /api/expenditure` now return `fluxLowKcal/fluxHighKcal = null`, `ExpenditureCalculator.FluxHalfWidth` and `Algorithm:FluxTrendErrorKg` are gone (Report F §0). The audit below describes the code as it was when audited.

Date 2026-10-23. Scope: read-only audit of the existing Flux and of the expenditure engine it hangs on. **No code, schema, data or production behaviour was changed.** Evidence tags: **CODE** (file:line read in this tree), **MF-OFFICIAL** (official MacroFactor page, short quote), **OBSERVED** (screenshot in `Docs/MacroFactor_UI`), **UNKNOWN**. The existing unit tests were NOT taken as proof of correctness: they only assert arithmetic (see §4).

## 1. How the engine works (the thing a new Flux must sit next to)
| Stage | What it does | CODE |
|---|---|---|
| Intake observation | per day `KcalIn` = stored day total, but **null** when the day is marked incomplete, or has entries summing to 0 kcal without being a fasting day; **fasting = 0 kcal (valid)**; unlogged day = absent = null (never 0) | `FoodLogSupport.cs:45` (`AlgorithmKcal`), `:57-71` (`LoadDaysAsync(includeFasting)`) |
| Weight observation | `ExpenditureDay.TrendKg` = the stored **trend** weight, present **only on weigh-in days** (null otherwise) | `ExpenditureRebuilder.cs:35-41`, `ExpenditureDay.cs:5` |
| Trend weight | EMA (α = 2/(N+1), **N = 20**) run over a **daily series linearly interpolated between weigh-ins**; stored per weigh-in row | `WeightTrendCalculator.cs:7-33`, `AlgorithmOptions.cs:27-28` |
| Window | the last **W = 21 calendar days** ending at the day, never before `ExpenditureStartDate` | `ExpenditureRebuilder.cs:121`, `AlgorithmOptions.cs:29-30` |
| Raw TDEE | needs ≥ 2 weigh-in days in the window and ≥ 1 valid log **inside [first weigh-in, last weigh-in]**; `tdeeRaw = mean(KcalIn over valid days in that span) − (trendLast − trendFirst) × 7700 ÷ spanDays`. Missing days inside the span are implicitly imputed by the mean of the logged days | `ExpenditureCalculator.cs:12-29` (imputation comment `:23`) |
| Smoother | **EMA on the TDEE itself**: `tdee = β·raw + (1−β)·prevTdee`, **β = 0.1**. Raw null ⇒ the previous TDEE is carried | `ExpenditureCalculator.cs:44-45`, `ExpenditureRebuilder.cs:132`, `AlgorithmOptions.cs:45-46` |
| State machine (G0) | Updating while ≥ 4/7 valid-log days **and** ≥ 1 weigh-in in 7 days; Holding otherwise; resume after 3 consecutive valid days; Calibrating = a new series that has not yet been Updating; today without a valid log is "pending", not missing | `ExpenditureStatusMachine.cs:19-49` |
| Calibrating freeze | default `FreezeTdeeDuringCalibration = true`: for the first 14 days the status is forced to Calibrating and TDEE is not updated (the "learn from day 1" variant is a pending, unapproved flag) | `ExpenditureRebuilder.cs:127`, `AlgorithmOptions.cs:31-37` |
| Initial TDEE (TDEE₀) | snapshot on the program start day, `Status = Calibrating`, value from the Initial resolver (formula or manual) | `CreateProgramCommandHandler.cs:91-99` |
| Persistence | one `ExpenditureSnapshot` per day that had a data write: `EstimatedTdeeKcal, Status, AvgCaloriesConsumed, WeightChangeRateKgPerWeek, WindowDays, AlgorithmVersion` | `ExpenditureRebuilder.cs:145-150`, `ExpenditureSnapshot.cs:12-27` |
| Series | continuous daily series; days without a snapshot **carry the previous TDEE, status Holding, previous WindowDays** | `ExpenditureSeries.cs:12-27` |
| Static / Dynamic | Static is a per-day overlay: `Static ? staticValue : learned`; **not wired into any flow yet**, and never changes learned snapshots | `ActiveExpenditureResolver.cs:4-16`, `ExpenditureModeTimeline.cs:9-13` |
| Not inputs | goal, program targets, check-ins, Calories/Macros/Strategy do not enter the engine (only `ExpenditureStartDate` and the manual TDEE₀ come from the program) | `ExpenditureRebuilder.cs:16-22,48-56` |

So the **real production smoother is a two-stage estimator**: a 21-day windowed energy-balance estimate (itself built on a 20-day EMA weight trend), then an EMA (β = 0.1) on the TDEE. Any "responsiveness" candidate must vary **those two knobs (β, W)** on the same observations; it must not assume a single EMA on the TDEE or on weights.

## 2. Every Flux in the tree
| # | Where | What |
|---|---|---|
| F1 | `ExpenditureCalculator.cs:31-33` `FluxHalfWidth(windowDays, kcalPerKg, trendErrorKg) = Round(trendErrorKg × kcalPerKg ÷ Max(windowDays, 7))` | the only formula |
| F2 | `AlgorithmOptions.cs:47-48` `FluxTrendErrorKg = 0.1` | the only knob; **not overridden in `appsettings.json`** (Algorithm keys there: KcalPerKg, TrendDays, ExpenditureWindowDays, CalibratingDays, MaxWeeklyDeltaKcal, SmoothingBeta) |
| F3 | `ExpenditureSeries.cs:56-66` `ToDays` → `FluxLowKcal = tdee − half`, `FluxHighKcal = tdee + half` | used by `GET /api/expenditure/series` (`GetExpenditureSeriesQueryHandler.cs:15`) and by `EnergyBalanceSupport.cs:23` (which discards the band and keeps only `ExpenditureKcal`) |
| F4 | `GetExpenditureQueryHandler.cs:18-32` | same formula for `GET /api/expenditure` points (`FluxLowKcal/High` nullable there) |
| F5 | Contracts `ExpenditureSeriesDay.cs:12` (`decimal FluxLowKcal, FluxHighKcal`, **non-null**), `ExpenditurePointModel.cs:13-14` (nullable) | FE types `expenditure-series.types.ts:10-11`, `dashboard.types.ts:13-14` |
| F6 | Docs `Docs/Design/Expenditure.md:8,33-34,80`, `A7_Derived_Data_Rebuild_Design.md:153-162`, `Dashboard/Expenditure/README.md:20` | all describe it as "uncertainty"; `Expenditure.md:80` already admits "MacroFactor does not publish them" |
| F7 | FE does **not draw it**: `expenditure-series.ts:10-11`, `ExpenditureChart.tsx:31` ("công thức của BE không phải của MacroFactor") | already hidden |
| F8 | Tests `AlgorithmChecks/Program.cs:252-253, 1768-1769` | assert `37`, `110`, `1963/2037` — arithmetic only |
The same constant is also used elsewhere as a *tolerance* idea in `CheckIn_OnTarget_Tolerance_Proposal.md`, which already decided not to reuse it.

## 3. The six questions (verbatim), answered
**1. "Flux hiện tại tính từ đâu."** From `FluxHalfWidth` (F1): half-width = `round(0.1 kg × 7700 kcal/kg ÷ max(windowDays, 7))`, applied symmetrically around the stored TDEE, computed **on read** (nothing stored). A 21-day window ⇒ **±37 kcal**; a window of ≤ 7 days ⇒ **±110 kcal**.

**2. "Dữ liệu nào thực sự tham gia tính."** **Only `WindowDays`** plus two constants. `WindowDays = window.Count` = the number of **calendar days** in the 21-day window since the start (`ExpenditureRebuilder.cs:121,149`), regardless of whether any day in it had food or a weigh-in; carried for gap days (`ExpenditureSeries.cs:24`). It ignores: valid log count, weigh-in count, noise of the trend, size or speed of TDEE change, Holding/Updating, missing/incomplete/fasting days, and the **TDEE itself** (apart from being the centre). After day 21 the band is a **constant ±37 kcal for every user, forever** (≈ 1.5 % of 2500).

**3. "Có bị hardcode không."** Yes: `FluxTrendErrorKg = 0.1` (a global default, `AlgorithmOptions.cs:48`), the literal `7` floor (`ExpenditureCalculator.cs:33`), `KcalPerKg = 7700`, integer rounding, and the symmetry. In practice it is the "constant ± kcal" the requirement forbids; it can only be rescaled by one global knob.

**4. "Có phải uncertainty band nhưng gắn nhãn Flux không."** **Yes.** Its own contract text calls it "độ bất định của ước lượng" (`ExpenditurePointModel.cs:4`) and the docs call it uncertainty (F6): a propagated assumed trend error, not a family of trajectories. MF describes Flux differently (**MF-OFFICIAL**, help article 74): "Flux range is an indicator of change magnitude"; "the algorithm could have taken any path through that shaded area"; calling it a confidence interval "is not technically accurate"; "a larger flux range represents a larger potential for error, but this is not a direct relationship"; it is shown "for fun and curiosity". MF V3 can "navigate through the flux range intelligently instead of relying on a fixed route" (article 74); the formula is **not published** (article 20 and the V3 page say nothing about Flux).

**5. "Có dependency vào production TDEE calculation không."** **Not as an input:** nothing in the TDEE, status, check-in, targets or Calories/Macros reads Flux (grep: F3/F4 are the only consumers; `EnergyBalanceSupport` drops it). **As a source of coupling it reads one engine-written column** (`WindowDays`, `ExpenditureRebuilder.cs:149`). A replacement must stay an output-only consumer: no feedback into the learned estimator, no change to the G0 machine, β, W or the series carry rule.

**6. "Có schema/API hiện có để tái sử dụng không."** Partly:
- API: `GET /api/expenditure/series` already returns `fluxLowKcal/fluxHighKcal` per day (non-null), with `status` and `isCalibrating`; `granularity` buckets are built from `ExpenditureKcal` only (`GetExpenditureSeriesQueryHandler.cs:17`, `MetricSeries.Build`), so **buckets carry no band**. `GET /api/expenditure` points have nullable flux fields.
- Schema: `ExpenditureSnapshot` has **never-populated nullable columns** `ReasonCodesJson, QualityScore, Completeness, WindowStartDate, WindowEndDate` (`ExpenditureSnapshot.cs:23-27`; no writer in Application, only EF configuration). No columns exist for a per-day band or a policy version; today's band is not stored.
- Status already distinguishes Updating / Holding / Calibrating and `IsCalibrating` is a separate UI label.

## 4. Additional findings relevant to the new design (not assumed correct)
1. **Tests prove arithmetic, not validity** (F8). `37` and `110` follow from the formula; nothing checks that the band means anything.
2. **Estimator lag.** Δtrend uses an EMA trend (N = 20) and a 21-day window, then β = 0.1 on top: under a real expenditure ramp the learned TDEE lags and under-shoots. Report E measures this (MAE of the learned estimate) instead of assuming it.
3. **Trend memory ⇒ non-independent observations.** The stored trend at a weigh-in already contains earlier weigh-ins and interpolation across gaps (`WeightTrendCalculator.cs:23-28`), so successive windows share information and interpolated days are not independent measurements. A Flux must count only real weigh-in days as observations.
4. **Missing-day imputation** inside the span (`ExpenditureCalculator.cs:23`) means a window with few valid logs is a noisier estimate even though `WindowDays` is still 21.
5. **Two default behaviours coexist:** frozen 14-day Calibrating (current default) versus the unapproved "learn from day 1"; Flux must be defined for both (Calibrating ⇒ no band under the stated rule).
6. **Static overlay is not wired**, so Static-mode handling of Flux is a design decision made *before* the overlay exists (MF evidence: none).

## 5. MF evidence used (and what is missing)
- **MF-OFFICIAL:** help.macrofactorapp.com/en/articles/74-expenditure-version (Flux definition above; "pauses less frequently" than V1/V2; V3 minimum: nutrition on 4 of 7 days, weight once per week). The macrofactor.com/expenditure-v3 page and help article 20 contain **no** Flux text. Quotes were relayed by a page-summarising fetch tool; re-check wording before using any of it as UI copy.
- **OBSERVED** (`Docs/MacroFactor_UI/UI/Dashboard/Expenditure/1-chart-collapsed.png`, `Expenditure_v2/01_mf_chart_flux_range.png`): Flux is drawn as a shaded band behind the line **while the status is Holding** (square markers); in the 1W capture the band is about ±15 kcal around 2429 and flat; in the 3M capture the band's upper edge tapers (2445 → ~2431) and the lower edge rises (2414 → ~2421) around the last points where the line moves to ~2426. One account, a few days; not enough to infer a formula, width law, or a hide rule.
- **UNKNOWN:** whether MF hides Flux in Static mode; whether it draws it in Calibrating; how the band is aggregated for Weekly/Monthly averaging; what drives its width.
