# NDM ↔ Food Log ↔ Analytics ↔ Expenditure: STEP 1 audit + STEP 2 proposal (MOS-BE, 2026-10-25)

Spec: `Docs/Design/Specs/NDM_FoodLog_Integration_Spec.md`. Everything below is **read from code** (file:line); nothing was executed yet, the LocalDB reproductions come in STEP 4.
Note: the spec mentions `Mutation_Query_Matrix.md`; it does not exist in the repo, the equivalent is `BE/Docs/Design/Ops/Mutation_Dependency_Matrix.md`.

## 1. Data-flow (today)

```
POST/PUT/DELETE food-logs (AddFoodLogEntry, QuickAdd, Update/Delete entry, Batch, BulkCopy/Move/Delete, CopyFoodLog)
   │  entry rows (Calories/Protein/Carbs/Fat NOT NULL snapshots; Source enum)         FoodLogEntry.cs:25-30
   ▼
FoodLogSupport.RecalcTotals(log)  = sum of live entries  ──►  FoodLogs.Total{Calories,Protein,Carbs,Fat,Fiber}   FoodLogSupport.cs:22
   │  (+ IsFasting / IsIncomplete flags on the same row)                              FoodLog.cs:12-15
   ▼
FoodLogSupport.SaveAndRebuildAsync (FoodLogSupport.cs:174) ─► DerivedDataSync.RunAsync (lock, aggregates, ExpenditureRebuilder)
   ├─ NutritionAggregates.RefreshAsync: NutritionDayAggregate per day (sums ENTRIES again, not Total*)   NutritionAggregates.cs:19-29
   └─ ExpenditureRebuilder → ExpenditureSnapshot + DailyProgressSnapshot

READ side: ONE funnel for kcal/macros per day: FoodLogSupport.LoadDaysAsync → LoggedDay (FoodLogSupport.cs:35-71)
   ├─ ExpenditureRebuilder (kcal-in via LoggedDay.AlgorithmKcal; daily progress via raw TotalCalories)   ExpenditureRebuilder.cs:30,39,81
   ├─ EnergyBalanceCalculator            EnergyBalanceCalculator.cs:28,33
   ├─ MacroSeriesCalculator              MacroSeriesCalculator.cs:18-29
   ├─ NutrientDetail (energy_kcal)       NutrientDetailSupport.cs:28,86
   ├─ CheckInSupport                     CheckInSupport.cs:40-42,111
   ├─ GetWeekLog / GetExpenditure(+Insights)
   └─ NutritionOverview: LoadDaysAsync for the day set, then SUMS from NutritionAggregates (Month/Quarter/Year) or NutrientCatalog.SumsAsync (Today/Week)  GetNutritionOverviewQueryHandler.cs:22-30
NDM  GET /nutrition-data  = its own projection of the SAME FoodLogs row (Total* + Entries.Count + QuickAdd count)  GetNutritionDataQueryHandler.cs:8-26
GET /food-logs/{date}     = its own projection of the same row + entries                                          GetDayLogQueryHandler.cs:13-55
```

**Good news:** a canonical funnel already exists (`LoggedDay`/`LoadDaysAsync`, with `AlgorithmKcal`); NDM already reads the same `FoodLogs.Total*` that Add Food writes. We should evolve it, not add a parallel resolver.

## 2. The 10 questions

| # | Answer | Evidence |
|---|---|---|
| 1 | **Yes.** NDM GET reads `FoodLogs.Total*`, which every Food Log write handler recalculates in the same unit of work as the entry (`RecalcTotals` in Add, QuickAdd, Update, Delete, Batch, BulkCopy/Move/Delete, Copy, QuickEdit, NDM Save). | `GetNutritionDataQueryHandler.cs:8-19`, `FoodLogSupport.cs:22`, 12 `RecalcTotals` call sites |
| 2 | Same `FoodLogs` row as Food Log. There is **no** separate NutritionData table and no "manual override" column. | `NutritionDataController.cs`, `FoodLog.cs` |
| 3 | **No such case found.** NDM lists a day when `Entries.Any() \|\| IsFasting`; soft-deleted entries are excluded by the global query filter (`SoftDeleteEntityConfiguration.cs:11`). A 2000 kcal Food Log day shows 2000 (source `App`). Only odd case: a day whose entries are all 0 kcal shows **0 kcal / App** as if valid while Expenditure/Energy Balance treat it as "not a valid kcal day" (`FoodLogSupport.cs:45`). | as cited |
| 4 | **Yes, in one direction.** "Manual" is just a `FoodLogEntry` with `Source = QuickAdd` (written ONLY by `SaveNutritionData` and `QuickEditDay`). A Food Log food added later to a day that holds that entry is **summed** with it: Manual 2200 + food 300 ⇒ 2500, and the day flips to source `App`, after which NDM refuses to edit it (400). Contradicts "manual overrides, never additive". | `AddFoodLogEntryCommandHandler.cs:16`, `NutritionDataSupport.cs:5-11` |
| 5 | There is **no override concept to bypass**. The reverse problem exists: nothing reads Food Log items while a manual entry exists because the items were **soft-deleted** when the manual entry was written (B1). | `SaveNutritionDataCommandHandler.cs:25-32`, `QuickEditDayCommandHandler.cs:11-15` |
| 6 | Aggregates are refreshed in `DerivedDataSync` (inside the lock) or by the FoodLogChanged job. **Stale window** when the sync is queued (already G5 in the matrix). Aggregate staleness is never detected (only *missing* days self-heal). Water add/delete refresh the aggregate (`WaterSupport.cs:9`). No other mutation path misses it. | `NutritionAggregates.cs:97-111` |
| 7 | **Partly.** Expenditure uses `AlgorithmKcal` (null for incomplete days and for 0-kcal non-fasting days); Energy Balance applies the identical rule (`EnergyBalanceCalculator.cs:33,50`); Check-in uses `AlgorithmKcal` (`CheckInSupport.cs:41`). **Not the same**: Macro series counts every logged non-incomplete day, including 0-kcal non-fasting ones (`MacroSeriesCalculator.cs:29`), and the Nutrition Overview averages over `logged.Count` where `logged` excludes fasting days but includes incomplete and 0-kcal days (`GetNutritionOverviewQueryHandler.cs:22-23`; `LoadDaysAsync` default `includeFasting:false`). Daily Progress snapshot stores raw totals for every logged day (`ExpenditureRebuilder.cs:81`). So Overview, Macros and Energy Balance can show different averages for the same days (B4). | as cited |
| 8 | **No.** Nothing can be restored: the manual write soft-deleted the foods (still in the table with `IsDeleted=1`, restorable in principle), but "remove manual" = `DELETE /nutrition-data`, which **hard-deletes the whole FoodLog row** (cascade to entries, entry nutrients and water logs), see B2. And it is refused (400) whenever any non-QuickAdd entry exists. | `DeleteNutritionDataCommandHandler.cs:12-16`, `FoodLogConfiguration.cs:35-43` |
| 9 | Day = user-local `LogDate`; Save rejects future days by the user's timezone (`SaveNutritionDataCommandHandler.cs:10-11`); GET takes dates as given (`LogDate` is already local). Manual entry gets `EatenAt` from `ResolveTimeAsync` and `LoggedAtUtc = null`. No defect found; tested in STEP 4 (row 18). | as cited |
| 10 | **Yes, two kinds.** (a) `NutritionAggregates.PerDayAsync` self-heal writes `NutritionDayAggregate` rows and calls `SaveChanges` inside GETs: `GET /nutrition/overview` (Month/Quarter/Year, `GetNutritionOverviewQueryHandler.cs:29`) and the nutrient series/detail endpoints (`NutrientDetailSupport.cs:34`). (b) `GetFoodByBarcode` and `ResolveAiIngredient` save catalog/cache rows (not nutrition data). NDM GET and Food Log GET do not write. | grep of Query handlers that save |

## 3. Bugs / spec violations found (code-derived; the repro scripts are the STEP 4 tests)

| ID | Severity | Bug | Evidence | Repro (API sequence) |
|---|---|---|---|---|
| **B1** | **high (data loss)** | `PUT /food-logs/{date}/quick-edit` (the MF "Quick Edit full day") **soft-deletes every live entry of the day** (real foods and recipes included) and replaces them with ONE QuickAdd entry. The foods vanish from Food Log (no restore path). Spec §3.B: a manual total "không xóa món cũ". NDM Save does the same for QuickAdd-only days. | `QuickEditDayCommandHandler.cs:11-15`, `SaveNutritionDataCommandHandler.cs:25-32` | log 3 foods (1800) → quick-edit 2200 → `GET /food-logs/{date}` shows 1 entry; the 3 foods are gone |
| **B2** | **high (data loss)** | `DELETE /nutrition-data` (`DeleteNutritionDataCommandHandler.cs:15`) and `DELETE /food-logs/{date}` (`ClearDayLogCommandHandler.cs:11`) call `Repository<FoodLog>.RemoveAsync`; `FoodLog` is NOT `ISoftDeletable` ⇒ the row is **physically deleted**, and the DB cascade removes the day's entries, entry nutrients **and WaterLogs** (and loses `Notes`, `IsIncomplete`, `IsFasting`). "Remove Manual Override", "Delete Nutrition Data" and "Clear Food Log Day" are one destructive operation; removing a manual entry or fasting flag also erases that day's water. | `FoodLogConfiguration.cs:35-43`, `FoodLog.cs:5` | log water 500 ml + mark fasting → `DELETE /nutrition-data?date=` → `GET /water/{date}` is empty |
| **B3** | high (spec) | No manual-over-Food-Log override: NDM Save on a day with any non-QuickAdd entry returns 400 `DayLoggedInFoodLog`. Spec: manual 2200 overrides Food Log 1800. | `SaveNutritionDataCommandHandler.cs:15-17`, `NutritionDataSupport.cs:11` | day with 3 foods → `PUT /nutrition-data` calories 2200 ⇒ 400 |
| **B3b** | high (spec) | Adding a food after a manual entry sums with it (Q4). | `AddFoodLogEntryCommandHandler.cs` | manual 2200 → add 300 kcal food ⇒ day 2500, source flips to App |
| **B5** | medium | Calories-only manual stores protein/carbs/fat as **0** (`request.ProteinG ?? 0` …) and the entry columns are NOT NULL, so "macros not entered" is indistinguishable from "explicit 0". Macro series then draws "Other = kcal" and 0 g macros as data. Spec §3: "Không tự biến giá trị chưa nhập thành 0". | `SaveNutritionDataCommandHandler.cs:38`, `FoodLogEntry.cs:26-28` | `PUT /nutrition-data` calories 2200 only ⇒ macros 0 everywhere |
| **B4** | medium | Same days, different effective-day rules across consumers (Q7): Macro series and Overview count 0-kcal non-fasting days; Overview also drops fasting days and counts incomplete days in its divisor; Energy Balance/Expenditure/Check-in do not. | `MacroSeriesCalculator.cs:29`, `GetNutritionOverviewQueryHandler.cs:22-23`, `EnergyBalanceCalculator.cs:50` | day with only a 0-kcal entry: Macros avg includes it, Energy Balance avg excludes it |
| **B6** | low | GET write-on-read self-heal (Q10a). Safe (idempotent) but a GET that writes. | `NutritionAggregates.cs:100-107` | month overview on a user with entries but no aggregate rows |
| **B7** | low | `Source` is `App`/`Manual`, but "Manual" really means "all entries are QuickAdd"; the `Import` source in the contract is unused. Fine today, wrong once an override coexists with foods. | `NutritionDataSupport.cs:5-8`, `NutritionDataDayModel.cs:6` | n/a |
| — | note | Fasting + positive Food Log: unreachable today (`AddEntryAsync` clears `IsFasting`, `FoodLogSupport.cs:82`; NDM refuses fasting on App days). Manual 0 kcal without Fasting: allowed, stored as a 0-kcal entry, counted as "not a valid kcal day" by the algorithms (`FoodLogSupport.cs:45`), shown as 0 in NDM. | | |

## 4. STEP 2 proposal

### 4.1 Canonical abstraction: evolve `LoggedDay` / `LoadDaysAsync`, do not add a second resolver
`FoodLogSupport.LoadDaysAsync` already is "the effective daily nutrition" funnel for 10 consumers. Add to `LoggedDay`:

```
Source        : Manual | FoodLog | Imported | Fasting         (Missing = day absent from the dictionary)
Calories?, Protein?, Carbs?, Fat?   (null = not entered; NOT 0)
FoodLogCalories / FoodLogProtein / …  (what the foods alone sum to, for "Food Log total" labelling)
HasOverride   : bool
+ existing: IsIncomplete, IsFasting, AlgorithmKcal (rule unchanged: incomplete ⇒ null, 0 kcal without fasting ⇒ null, fasting ⇒ 0)
```
Rule (per DAY for the kcal, per FIELD for macros):
1. day has a valid manual override ⇒ Source=Manual; kcal = override; each macro = override value or **null** if not entered (never 0).
2. else ≥1 live non-override entry ⇒ Source=FoodLog; totals = sum of those entries.
3. else `IsFasting` ⇒ Source=Fasting; 0 kcal (valid).
4. else Imported: **no integration exists today ⇒ not built** (enum value reserved).
5. else Missing/Unlogged (never 0).
`AlgorithmKcal` stays the single validity rule; Macro series, Overview, Energy Balance, Expenditure, Check-in, Daily Progress all derive from it ⇒ B4 closes by construction.

### 4.2 Where the override lives, in two tiers
* **Tier 0, NO schema change (recommended first):** the override is the existing `FoodLogEntry` with `Source = QuickAdd` (already the only marker; written only by NDM Save and QuickEdit). Changes: (a) **stop soft-deleting foods** when it is written (B1/B3): foods stay live in Food Log and the day's effective totals come from the override entry only; (b) `RecalcTotals` becomes the ONE place that decides: `FoodLogs.Total*` = **effective** totals (override entry if present, else sum of non-override entries), and `NutritionAggregates.RefreshAsync` uses the same selection (today it re-sums entries, `NutritionAggregates.cs:19-29`); (c) Add/Edit/Delete food while an override exists keeps the override authoritative (no sum, no flip) and just updates the food list + `FoodLogCalories`; (d) "Remove Manual Override" = soft-delete that one entry ⇒ `RecalcTotals` falls back to the foods, **1800 restored with history intact**; (e) split the destructive endpoints (B2): Remove Override, Clear Food Log Day (soft-delete entries only, keep water/notes/flags unless asked), Delete Nutrition Data, Mark Incomplete, Mark Fasting become separate operations, none hard-deletes `FoodLogs`.
* **Tier 1, needs a migration (PROPOSAL ONLY, not created or applied):** to represent "macro not entered" honestly we need a nullable signal. Smallest option: `FoodLogEntry.MacrosEntered bool NOT NULL DEFAULT 1` (false only for a calories-only override). Without it a calories-only manual can only be reported as macros = 0 (current behaviour) or heuristically null (a fake: refused). Alternative: make `Protein/Carbs/Fat` nullable on the entry (touches every `Sum`), larger blast radius. No new table is needed for any of this.

### 4.3 Consumers that must switch
Already on the funnel (get the new fields once `LoggedDay` changes): ExpenditureRebuilder, EnergyBalance, MacroSeries, NutrientDetail, CheckInSupport, GetWeekLog, GetExpenditure, Insights.
Need explicit change: **GetNutritionData** (own projection ⇒ use the funnel, expose Source and null macros), **GetDayLog** header totals (+ `foodLogTotals`), **NutritionOverview** (use `AlgorithmKcal`/validity rule and the same divisor as Macro series; the Today/Week path `NutrientCatalog.SumsAsync` must not add food macros under an override), **NutritionAggregates.RefreshAsync**, **DailyProgress snapshot** (store effective values), **SaveNutritionData / QuickEditDay / Delete / ClearDay** handlers, Overview micro-nutrient cards (label "from logged foods").

### 4.4 API contract changes (additive; FE must not compute totals itself)
`GET /nutrition-data` item: keep `calories/proteinG/fatG/carbsG` but **macros become nullable**; `source` ∈ `Manual | FoodLog | Fasting | Imported` (replaces `App`; BE can emit the legacy value too during the transition); add `hasOverride`, `foodLogCalories`, `foodLogProteinG|CarbsG|FatG`, `isValidForAlgorithms` (= `AlgorithmKcal != null`); keep `isFasting`, `isIncomplete`, `entryCount`. Days with no data stay absent ⇒ FE renders "Unlogged", never 0.
`PUT /nutrition-data`: `null` ≠ `0` for macros; allowed on days with foods (creates the override); response = the new effective day. A PUT whose values equal the current effective values **must not create an override** (spec §5: open Edit Day then Save changes nothing).
New: `DELETE /nutrition-data/{date}/override` (only the override); `DELETE /food-logs/{date}` stays "clear food log day" but must stop deleting water/flags; fasting and incomplete flags either as `PUT /nutrition-data/{date}/fasting|incomplete` or kept in the PUT.
`GET /food-logs/{date}`: totals = effective, plus `source`, `foodLogTotals`, `hasOverride`.
`X-Derived-Data-State`: unchanged.

### 4.5 Schema: is ANY change needed?
Tier 0: **no** (reuse `FoodLogEntry.Source = QuickAdd`). Honest macro-null: **yes, one small column** (Tier 1). I will not create or run a migration; it needs the user's approval.

### 4.6 Answers to MOS-FE's follow-up (added 2026-10-25)
**(a) Which model does the BE implement?** The *entry-inside-Food-Log* model of `Docs/Design/Nutrition_Data_Manager.md:15,43`: a manual day = ONE `FoodLogEntry` (`Source = QuickAdd`, named "Manual entry", `Quantity = 100` g of a per-100 g `FoodItem` created per save) that replaces the day's previous entries: `SaveNutritionDataCommandHandler.cs:25-52` (replace), `QuickEditDayCommandHandler.cs:11-41` (replaces ALL live entries, foods included). It is NOT an override: it is summed like any entry (Q4). Two doc/code mismatches: (1) the doc says Quick Add entries made through Food Log's Quick Add count as Manual (`Nutrition_Data_Manager.md:51`), but `QuickAddCommandHandler.cs:23-31` never sets `Source`, so they are `Search` ⇒ an App day (NDM locks it). `Source = QuickAdd` is written only by NDM Save and QuickEditDay. (2) every Save creates a new "Manual entry" `FoodItem` row (`SaveNutritionDataCommandHandler.cs:37-39`), a catalog row per edit.
**What the spec's model needs.** Two ways; both keep Food Log entries untouched:
* **Option A, no/minimal schema (Tier 0 above):** keep the override as the `QuickAdd` entry but exclude it from the food sum and let it win in `RecalcTotals`/aggregates. Carries everything except null macros (needs `MacrosEntered`, Tier 1). The override still lives in the entries table and still creates a `FoodItem` per save (can be fixed by reusing one per user).
* **Option B, matches the spec literally (needs ONE migration, proposal only):** four nullable columns on `FoodLogs`: `ManualCalories`, `ManualProteinG`, `ManualCarbsG`, `ManualFatG` (override present ⇔ `ManualCalories IS NOT NULL`). Food Log entries are never touched; no phantom entry or `FoodItem`; macros are null natively (answers (e)); Remove Override = set the four columns to NULL (Food Log total returns instantly, history intact); `RecalcTotals` keeps summing foods into `Total*` (= `foodLogTotals`) and the resolver picks Manual over them. Existing manual days (QuickAdd-only) keep working: the resolver treats a day with no columns set as before, so no data migration is required (legacy entries can be converted later, optional). **My recommendation: B** if the user approves the migration; A is the no-migration fallback. Either way the existing `FoodLogs.Total*` and `LoggedDay` consumers change in one place.
**(e) Calories-only manual: confirmed.** `SaveNutritionDataCommandHandler.cs:38` `request.ProteinG ?? 0` (same for carbs/fat) and `FoodLogEntry.Protein/Carbs/Fat` are NOT NULL (`FoodLogEntry.cs:26-28`), so a calories-only day is stored and shown as 0 g macros; Macro series then derives `Other = kcal` (`MacroSeriesCalculator.cs:23`). Null-preserving option: Option B (native nulls) or Option A + `MacrosEntered`. Downstream rule for null macros: macro kcal/percent for that day = null (not 0), `Other` = null (not "all kcal"), and per-nutrient averages use only days where that macro is known (per-field denominator), Energy Balance/Expenditure unaffected (kcal only).
**(f) Incomplete-only day omitted from `GET /nutrition-data`: confirmed.** The list filter is `Entries.Any() || IsFasting` (`GetNutritionDataQueryHandler.cs:8`), so a row with only `IsIncomplete = true` is omitted. Expenditure/Energy Balance see such a day as *unlogged* (not in `LoadDaysAsync`), i.e. the same exclusion, so no calculation mismatch. The state is not directly creatable: `SetIncompleteDays` flags only days with entries (`SetIncompleteDaysCommandHandler.cs:24`) and NDM Save refuses a flag-only write on an empty day (400 `NutritionDataNeedsEnergy`, `SaveNutritionDataCommandHandler.cs:22-23`). It arises when the last entry of a flagged day is deleted (`DeleteFoodLogEntryCommandHandler.cs:9-12` does not clear the flag). That exposes two small bugs:
* **B8 (low):** the stale `IsIncomplete` survives delete-all; if the user later logs food on that day it is silently excluded from Expenditure/Check-in without being re-marked. Fix option: clear the flag when the last live entry is removed (technical, but it is also a business transition, so I treat it as U5: report and wait).
* **B9 (low):** `PUT /nutrition-data` always writes `log.IsIncomplete = request.IsIncomplete` (default false) (`SaveNutritionDataCommandHandler.cs:56`), so a PUT that omits the flag silently clears it. FE must always send the current flag; contract note, no BE change needed (or make it nullable: null = keep).
**Bounds on `GET /nutrition-data`.** BE already accepts optional `from`/`to` (`NutritionDataController.cs:11`) but with neither it returns every day ever logged, each with a correlated `Count()` ×2 (`GetNutritionDataQueryHandler.cs:16-17`), over a remote DB. Proposal (additive): default window = last 90 days when nothing is passed, `limit` (default 120, max 366) and `before` (exclusive cursor date, newest first), response adds `hasMore` and `nextBefore`; FE loads older pages on scroll. `from/to` stay for explicit ranges. No schema change (an index on `(UserId, LogDate)` already exists, `FoodLogConfiguration.cs:31`).

## 5. Business UNKNOWNs (report and wait, per the spec)
U1 calories-only manual: macros null vs derived vs 0 (BE can implement null once Tier 1 is approved). U2 add/edit/delete a food while an override exists: the proposal keeps the override authoritative and shows the food as "not counted"; **needs the user's decision** (MF behaviour unverified). U3 what the Food Log screen shows under an override. U4 micronutrients under an override: proposal "from logged foods, labelled", no scaling, no invention. U5 the §8 conflicts (fasting vs Food Log, manual>0 vs fasting, incomplete→complete, clear food log after fasting): today unreachable or refused; transitions need approval before changing. U6 should "Clear Food Log Day" delete water logs? (today yes, by cascade). U7 should Overview average fasting days as 0-kcal days like Energy Balance does?

## 6. Not changed
No TDEE / Weight Trend / algorithm change, no migration, no data written, no commit/push.
