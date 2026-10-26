# BE Release Candidate readiness report (MOS-BE, 2026-10-26)

Scope: Decisions_2026-10-26_round3_Release_Candidate.md §1–§5. BE tree at `main` (`08a16ec` + uncommitted work below). Nothing committed, pushed, deployed or applied to production by MOS-BE.

## 1. Uncommitted BE changes, grouped and mapped to approved decisions

| # | Group | Main files | Approved by |
|---|---|---|---|
| G1 | Lost entries + food search speed: per-user write lock (`FoodLogWriteLockBehavior`), batch idempotency (409 on reused key), in-memory catalog index (`FoodCatalogIndex`, `FoodSearchContext`, `FoodSearchPrefs`), plural-only `FoodSearchSynonyms`, UnitOfWork transaction fix, index warm-up at startup, `Application.csproj` + `Microsoft.EntityFrameworkCore.Relational 10.0.3` | `Application/Behaviors`, `Features/Foods/SearchFoods`, `FoodLogs/Batch`, `UnitOfWork.cs`, `ApplicationBuilderExtensions.cs` | User request "optimize food search / 4 foods gone", E2 round 1–2 (synonyms limited to plurals) |
| G2 | NDM v2 / S29: Manual Nutrition on `FoodLogs` (4 columns + CHECK), effective daily nutrition in day log / week / nutrients / macro series / energy balance / check-in / daily progress, `PUT .../fasting\|incomplete`, `DELETE .../override`, `reasonCode` on 400/409, paging on `GET /nutrition-data` | `Domain/.../FoodLog.cs`, `FoodLogConfiguration`, migration `20261025103001_S29_ManualNutrition`, `Features/NutritionData`, `Contract/Models/FoodLogs`, `GlobalExceptionHandler` | NDM rounds 1–4; **S29 deploy approval: see §5** |
| G3 | Expenditure: Holding/Calibrating reason codes (`ExpenditureDiagnostics`), Flux code gated OFF, bucket model, comment corrections | `Features/Expenditure/*`, `Contract/Models/Expenditure`, `AlgorithmOptions.FluxRangeEnabled=false` | E1 / E3 decisions (round 1 §1, §4; round 2 §8) |
| G4 | Egg catalog rows (Egg white raw [rep], cooked no added fat, Egg yolk raw), USDA wording, no "boiled" alias | `CanonicalCatalogData.Condiments.cs` | E2 round 1 (relabel), round 2 §5 (no global boiled=cooked) |
| G5 | Startup flag `Database:AutoMigrate` (default true = today's behaviour) | `MigrationStartup.cs`, `ApplicationBuilderExtensions.cs` | Round 3 §2 "startup must not auto-apply" (code ready, default unchanged) |
| G6 | Tests: AlgorithmChecks (869), ExpenditureHoldingRepro, FoodLogLostEntriesRepro, FoodSearchBench, NdmSafetyRepro, DerivedDataRepro, ExpenditureWhatIf (harness only) | `tests/*` | verification of G1–G5 |
| G7 | Docs: Ops proposals and readiness packs, S29 SQL scripts | `Docs/Design/Ops/*` | requested proposals |

## 2. Proofs requested by the audit (§3)

| Claim | Evidence |
|---|---|
| Flux disabled | `AlgorithmOptions.FluxRangeEnabled { get; init; } = false`; `grep -i flux src/API/appsettings*.json` = no match (the only config block is `Algorithm: KcalPerKg, TrendDays, ExpenditureWindowDays, CalibratingDays, MaxWeeklyDeltaKcal, SmoothingBeta`); `ExpenditureFluxService.LoadBandsAsync` returns no bands when off; repro **M5** proves series, buckets, `GET /expenditure`, insights return null bands and `fluxModel` null |
| ON_TARGET classifier inactive | `RateClassificationEnabled { get; init; } = false`, not set in any appsettings; diff of `AlgorithmOptions.cs` only adds `FluxRangeEnabled` |
| Production TDEE estimator unchanged | `git diff` of `ExpenditureCalculator.cs`, `AlgorithmVersions.cs`, `Bmr/*` = empty; `ExpenditureRebuilder.cs` diff = only the daily-progress source (`log.Calories` effective instead of `TotalCalories`), the TDEE line `ExpenditureCalculator.Smooth(prevTdee, raw…)` and `ComputeRaw` call untouched; `FreezeTdeeDuringCalibration` default `true`, `CalibratingDays` 14, `SmoothingBeta` 0.1 unchanged; AlgorithmVersion label unchanged |
| No experimental flag on by default | only new options: `FluxRangeEnabled=false`, `Database:AutoMigrate` default true (= today). `BackgroundDerivedProcessingEnabled` unchanged (true) |
| No test-only code in prod paths | `src/` grep for repro/whatif/bench/TestUser/`#if DEBUG\|TEST` = no hit in changed files; test projects only under `tests/`; the only `src` addition made for tests is the public `MigrationStartup` (used by the app) |
| §7 future-log rules have tests | `ExpenditureHoldingRepro` **O1** (QuickAdd/Copy to future days: Expenditure, snapshots, daily-progress unchanged), **O2** (check-in unchanged), **O3** (future-only logs ⇒ logged7 = 0, not Updating), **O4a/O4b** (UTC+7 and Los Angeles at the same instant, local "today" decides); `FoodLogLostEntriesRepro` **10** (copy to a future day, BulkMove, ClearDay: only explicit deletions) |
| Behaviour change to be aware of | `DailyProgressSnapshot.IsLogged` now follows effective kcal (a FoodLogs row whose entries are all deleted is "not logged" instead of "logged 0 kcal") and only updates when a snapshot is rebuilt; no TDEE number changes |

## 3. Verification run (isolated databases only)

`dotnet build src/API -c Verify`: 0 errors, 0 warnings. `AlgorithmChecks` 869 PASS. `FoodSearchBench`, `ExpenditureHoldingRepro` (A–O), `FoodLogLostEntriesRepro` (1–10), `NdmSafetyRepro` (incl. S29 up/idempotent/compat/restore/down and the AutoMigrate flag 27f), `DerivedDataRepro` ALL PASS. `ef has-pending-model-changes`: none. S29 is the only migration not on production (prod: 31 applied through S28).

## 4. Integration tests (§4): coverage and status

**API-level run against a live server: NOT executed.** Reason: the user asked in this session to test on the production DB and apply S29 there with throwaway accounts; the attempt to start the published BE against the prod DB was blocked by the session's permission classifier (no workaround attempted), and the user chose prod instead of the isolated LocalDB API, so no server was started. The launcher and publish are prepared (§7).

Handler-level coverage on isolated LocalDB (real handlers, real SQL Server, same code path as the API minus HTTP/auth):

| Area | Where it is exercised |
|---|---|
| Onboarding → Create Program | AlgorithmChecks onboarding/preview (11 checks) + program draft/feasibility checks; **gap**: no HTTP-level create-program flow |
| Food search → Food log | `FoodSearchBench` (Q1–Q10, L1–L6 equivalence with the old handler, paging), `FoodLogLostEntriesRepro` 1–9 (search-then-log via handlers and via real `IMediator` pipeline) |
| Add / Edit / Delete | `FoodLogLostEntriesRepro` 1–10, `NdmSafetyRepro` |
| Partial / Incomplete days | `ExpenditureHoldingRepro` G, `NdmSafetyRepro`, AlgorithmChecks (incomplete/fasting), scenarios F/H in `ExpenditureWhatIf` |
| Weekly Nutrition data | AlgorithmChecks (week log, effective nutrition, provenance), `NdmSafetyRepro` |
| Scale weight → Trend | AlgorithmChecks (47 weight/trend checks), `DerivedDataRepro` |
| Expenditure Holding / Updating | `ExpenditureHoldingRepro` A–N, AlgorithmChecks StatusMachine (68 checks) |
| Strategy → Check-in | AlgorithmChecks (19 check-in checks), `DerivedDataRepro` G6, `ExpenditureHoldingRepro` O2 |
| Historical targets | AlgorithmChecks provenance/LEGACY (14 checks) |
| Expenditure settings | AlgorithmChecks (settings); **gap**: thin (1 check) |

## 5. Final readiness (for MOS-Master's report)

### Completed and verified
G1–G5 above with the suites in §3; S29 SQL fully reviewed (up and down), CHECK semantics on NULL rows proven (and old BE compatible on the S29 schema); AutoMigrate flag implemented and tested; Flux off, estimator and defaults unchanged.

### Business decisions pending (no behaviour change made)
Confirm incomplete days (options A/B/C, `Expenditure_Partial_Logging_Proposal.md`); long-weight-gap rule (B0/B1/B2) and the nutrition-resume `3` (`Expenditure_Resume_Split_Proposal.md`); USDA online search contract/cache/import and the egg-white `boiled` alias, `SourceDataType` column (`Food_Online_Search_Proposal.md` §8); Coached protein lean-mass factors and fallback (`Protein_Coached_Lean_Mass_Audit.md`); future-date guard (user: no guard); Flux validation (stays off).

### Database migration blockers
S29 is not applied to prod. In-session the user approved "Apply S29 + test on prod"; it was not executed (permission block). Before applying: full backup (user, MonsterASP panel/BACPAC), preflight SELECTs (`S29_Production_Readiness.md` §12.1), decide `Database__AutoMigrate` (§13.3). Rollback is guarded (never drops manual values). Note that a push to `main` auto-applies S29 AND the idempotent seeders (3 new egg catalog rows) on startup unless the flag is set to false first.

### FE/BE compatibility
FE `2df9854` + new BE + S29: works for existing data (nullable-only changes, additive endpoints); FE v2 requires the new BE; new BE on S28 fails (or refuses to start with the flag off). Order: S29 → new BE → new FE immediately. Old BE `08a16ec` runs on the S29 schema (rollback safe). Details `S29_Production_Readiness.md` §12.3, §13.4–13.5.

### Integration results
Handler-level isolated-DB suites all green (§3). API/HTTP-level run not executed (§4). UI-level run belongs to MOS-FE.

### Release blockers and order
- **P0-1** S29 on prod: needs backup + preflight + the apply itself (or a push with auto-migrate), none executed. Handle first.
- **P0-2** API-level smoke on a live server (register throwaway user → onboarding → program → food search/log → weight → expenditure → check-in) not run. Run after S29 (or on LocalDB). See §7.
- **P1-1** Decide `Database__AutoMigrate=false` on the host before the first deploy (prevents unsupervised S29).
- **P1-2** Seeders write 3 egg catalog rows to prod at startup (unverified, USDA wording); acceptable only if intended.
- **P1-3** Background nightly derived job (default ON on prod) will rebuild snapshots with the new effective-nutrition semantics for real users; TDEE values unchanged, `IsLogged` semantics change (see §2). Decide whether to accept, or set `Algorithm__BackgroundDerivedProcessingEnabled=false` for the first deploy.
- **P2** Pending business decisions above; thin settings test coverage; no HTTP-level program-creation test.

## 7. Prepared but not run: API smoke on the production DB (only after the user runs/approves it)

Scratch files (outside the repo): published API in `…/scratchpad/rc/publish`, launcher `prod-rc.ps1` (prod connection string from the published appsettings; throwaway JWT key; background jobs off; email/cache offline; CORS only `http://localhost:3002`; never :3000). Throwaway accounts must use the prefix `rc-test-` so cleanup is exact; cleanup must delete those users' rows (6 tables reference `Users` without cascade: `CheckInDecisions`, `DailyProgressSnapshots`, `ExpenditureSnapshots`, `ExportJobs`, `WaterLogs`, `WeeklyCheckIns`; the rest cascade). Baseline before start (2026-10-26): migrations 31, Users 2, FoodLogs 11, FoodLogEntries 81 (59 soft-deleted), WeightEntries 24, ExpenditureSnapshots 11, DailyProgressSnapshots 11, MacroPrograms 4, MacroProgramWeeks 4, FoodItems 11871, FoodAliases 2061, CanonicalFoods 177, plus CHECKSUM_AGG of the real tables.
