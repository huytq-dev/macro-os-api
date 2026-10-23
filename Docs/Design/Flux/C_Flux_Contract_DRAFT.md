# Report C (DRAFT) — Flux contract, aggregation, storage

Date 2026-10-23. **Draft for review only; nothing is implemented, no migration file exists, no FE change.** It becomes relevant only if the user chooses option O2 (or a later variant) in Report B §8. Workflow stated by the user: approve formula + parameters first, then implement BE and FE on **one contract**.

## 1. Existing contract (CODE)
| Item | Today |
|---|---|
| `GET /api/expenditure/series?from&to&all&granularity` | `ExpenditureSeriesDay(Date, ExpenditureKcal, FluxLowKcal, FluxHighKcal, Status, ChangeKcal, IsCalibrating)` — `FluxLowKcal/FluxHighKcal` **non-null decimals** from the legacy ±37 formula (`ExpenditureSeriesDay.cs:12`). Buckets (`granularity`) are built from `ExpenditureKcal` only: **no band in buckets** (`GetExpenditureSeriesQueryHandler.cs:17`). |
| `GET /api/expenditure` | `ExpenditurePointModel(... FluxLowKcal?, FluxHighKcal?, IsCalibrating)` (nullable) |
| status | `Updating / Holding / Calibrating` per day already present; gap days carry the previous TDEE as Holding (`ExpenditureSeries.cs:20-26`) |
| FE | types contain `fluxLowKcal/fluxHighKcal` (`expenditure-series.types.ts:10-11`) but **does not draw them** (`expenditure-series.ts:10-11`) |

## 2. Proposed additive fields (series day and point)
| Field | Type | Meaning |
|---|---|---|
| `fluxLowerKcal` | int? | `Lower` of `MACROOS_FLUX_V1`; **null** when state is Calibrating / NoObservations / row predates the policy |
| `fluxUpperKcal` | int? | `Upper`; same nulls |
| `fluxModel` | string? | `"MACROOS_FLUX_V1"`; null when no band. Lets old rows and future policy versions coexist |
Existing `status`, `isCalibrating` already tell the FE Updating vs Holding vs Calibrating; no extra `fluxState` is proposed (derivable). **Compatibility choice (needs a decision):**
- **A (recommended):** add the three new nullable fields; keep `fluxLowKcal/fluxHighKcal` untouched for old clients, mark them deprecated in the XML docs, remove in a later, separately approved release. No type change for existing consumers.
- B: repurpose the existing names — would silently change their meaning and nullability (breaking for typed clients). Not recommended.
Rules in the DTO: `fluxLowerKcal ≤ expenditureKcal ≤ fluxUpperKcal` whenever both are present; Holding days repeat the last band; gap days carry it with `status = Holding`.

## 3. D/W/M aggregation (UNKNOWN in MF)
Today buckets only average kcal. Options for the band of a bucket:
| Option | Definition | Properties |
|---|---|---|
| **a (proposed)** | mean of the daily `Lower` and mean of the daily `Upper` over the same counted days as the bucket's kcal average | consistent with the averaged line; width stays comparable to the daily width |
| b | min of `Lower`, max of `Upper` in the bucket | strictly contains every day, but widens with bucket length (a month looks wider than a day) |
Days without a band (Calibrating/NoObservations) are skipped; a bucket with none has null. **Decision needed (UNKNOWN in MF).**

## 4. Pagination / size
`/series` is not paged today (`from`/`to`/`all`, default 90 days). The band adds two ints per day; a multi-year `all=true` is a few thousand rows (≈ 2 × 4 bytes + names per row of JSON). No pagination change is proposed; add a max-range guard only if `all=true` size becomes an issue (separate decision).

## 5. Where the band comes from (storage options — prepare only)
| Option | How | Pros / cons | Schema |
|---|---|---|---|
| **S1 (proposed if activated): compute at rebuild, store per snapshot** | `ExpenditureRebuilder` (unchanged logic for TDEE) additionally calls the calculator for the rebuilt days and stores `FluxLowerKcal`, `FluxUpperKcal`, `FluxPolicyVersion` (+ the K candidate values as a small JSON so an incremental rebuild can continue without reloading the whole history) | reads are cheap; consistent with how TDEE is stored; history is versioned; rebuild semantics already handle backdated edits (test S16) / cons: writes more per rebuild; old rows have null band until rebuilt (and the brief forbids an automatic historical rebuild) | **yes** — 3–4 nullable columns on `ExpenditureSnapshot` (additive) |
| S2: compute on read | the series handler loads the full nutrition + weight history and runs the calculator | no schema / cons: reads all history per request, risk of drift from stored TDEE, heavier | none |
| S3: nightly job | cons: another worker path and staleness | — | yes |
Existing never-populated nullable columns on `ExpenditureSnapshot` (`QualityScore, Completeness, WindowStartDate, WindowEndDate, ReasonCodesJson`, `ExpenditureSnapshot.cs:23-27`) have different meanings and are **not** reused.

**Migration sketch (NOT created, NOT approved):**
```sql
ALTER TABLE [ExpenditureSnapshots] ADD [FluxLowerKcal] int NULL, [FluxUpperKcal] int NULL, [FluxPolicyVersion] nvarchar(24) NULL, [FluxCandidatesJson] nvarchar(200) NULL;
-- Down: drop the four columns. No data is read or rewritten by Up. No index.
```
Old rows stay NULL (no band) until a normal rebuild reaches them; no automatic rebuild is part of this proposal.

## 6. Behaviour table for the FE (same contract for BE and FE)
| `status` | `fluxLowerKcal/Upper` | Draw |
|---|---|---|
| Calibrating | null | no band |
| Updating | present | band + circle marker |
| Holding | present (frozen last band) | band + square marker (proposed; **UNKNOWN** whether to hide after N days) |
| Static mode active | present or hidden — **UNKNOWN**; proposal: FE hides while Static is the displayed line | — |
| no valid observations | null | no band |

## 7. Tests the contract would need (not written)
Contract-additive check (old fields unchanged, new fields nullable); Holding/Calibrating/null rules; `Lower ≤ TDEE ≤ Upper` property; bucket aggregation per the chosen option; rebuild from a backdated day touches only days ≥ that day; FE `check-contract`.
