# NDM ↔ Food Log: P0 data-loss fix report + S29 design for approval (MOS-BE, 2026-10-25)

Binding inputs: `Docs/Design/Specs/NDM_FoodLog_Integration_Spec.md`, `Docs/Design/Specs/NDM_Decisions_2026-10-25.md`. Earlier audit: `NDM_Audit_And_Resolver_Proposal.md`.
**Nothing here touches a shared/prod DB. No commit/push.**
> **Status update 2026-10-25 (round 2):** the user approved the S29 design for implementation on LocalDB ONLY. Implemented in source: migration `20261025103001_S29_ManualNutrition` (4 nullable `decimal(8,2)` columns + CHECK; **not applied anywhere shared**), the resolver (`LoggedDay` effective vs raw), Contract v2 and the handlers; the P0 "interim" behaviour of sections 2 and 8 below (entry-based manual entry, 409 `MANUAL_OVERRIDE_UNAVAILABLE`) is **superseded**. Current behaviour and contract: `Docs/Design/Nutrition_Data_Manager.md`; tests: `tests/NdmSafetyRepro` (35 checks incl. migration Up/Down + backup/restore). Sections 1 (the two data-loss bugs) and 5–6 (legacy audit, S29 design) remain the reference.

## 1. The two data-loss bugs (exact)

### B1: Quick Edit / NDM Save deleted Food Log entries
| | |
|---|---|
| Endpoints | `PUT /api/food-logs/{date}/quick-edit` (`FoodLogsController.cs:141-144`) and `PUT /api/nutrition-data[/{date}]` (`NutritionDataController.cs:18-25`) |
| Old code | `QuickEditDayCommandHandler.cs:11-15` soft-deleted EVERY live entry of the day (foods and recipes included) and added one QuickAdd entry; `SaveNutritionDataCommandHandler.cs:25-32` did the same to the day's QuickAdd entries (NDM refused only on days that had a non-QuickAdd entry). |
| Repro (LocalDB, real handler, pushed commit `08a16ec`) | day with 10 foods (1800 kcal) → quick-edit 2200 → live 1, **deleted 10**, total 2200. NDM Save on a manual day: a new "Manual entry" food and entry each time, the old entry soft-deleted. Fasting on a day with a manual entry: manual entry deleted. |
| Impact | The day's Food Log history disappears from the UI (rows stay with `IsDeleted=1`, no restore path); totals, aggregates and expenditure are silently recalculated from the replacement; no confirmation. Reachable from any client that calls Quick Edit (Food Log "Edit Day"). |

### B2: Delete Nutrition Data / Clear Day hard-deleted the whole FoodLog row
| | |
|---|---|
| Endpoints | `DELETE /api/nutrition-data[?date= \| /{date}]` (`NutritionDataController.cs:27-35`) and `DELETE /api/food-logs/{date}` (`FoodLogsController.cs:21-24`) |
| Old code | `DeleteNutritionDataCommandHandler.cs:15` and `ClearDayLogCommandHandler.cs:11` called `Repository<FoodLog>.RemoveAsync`. `FoodLog` is not `ISoftDeletable`, so the audit interceptor does not convert it (`AuditSaveChangesInterceptor.cs:49-53`); the row is physically deleted and the FK cascades (`FoodLogConfiguration.cs:35-43`) remove entries, entry nutrients and **WaterLogs**; `Notes`, `IsIncomplete`, `IsFasting` go with the row. |
| Repro (pushed commit) | day with 3 foods + manual + 750 ml water + a note + incomplete flag → Clear: row gone, water 0, flags gone, a second Clear = 404. Delete NDM on a manual day: row gone, water gone. |
| Impact | One handler served four different user intents (Remove Override, Clear Food Log Day, Delete Nutrition Data, clear Fasting/Incomplete); removing a manual entry or the fasting flag also erased the day's water, day note and flags. |

## 2. What was changed (P0, no migration)

| Operation | Endpoint | Scope now (nothing else is touched) |
|---|---|---|
| Quick Edit day | `PUT /food-logs/{date}/quick-edit` | creates ONE manual entry on an empty day, or edits the single existing manual entry **in place** (same Id/meal/time; the old per-100 g food row is kept, the entry points to a new one). Day with foods or several entries ⇒ **409 `MANUAL_OVERRIDE_UNAVAILABLE`**, nothing changed. Never deletes. |
| NDM Save | `PUT /nutrition-data[/{date}]` | same rule as Quick Edit; fasting on a day with entries ⇒ **409 `FASTING_NEEDS_EMPTY_DAY`**; a day that has foods keeps the old 400 `DayLoggedInFoodLog`; saving the numbers already stored writes nothing. |
| Clear Food Log Day | `DELETE /food-logs/{date}` | soft-deletes the FOODS (entries that are not the manual entry). Keeps the row, manual entry, water, notes, fasting/incomplete flags. 404 if no row; no foods ⇒ success, no write (idempotent). |
| Delete Nutrition Data | `DELETE /nutrition-data[?date=\|/{date}]` | soft-deletes the manual entry and clears the fasting flag. Keeps the row, foods, water, notes, incomplete flag. Day with only foods ⇒ 400 (as before); no row ⇒ 404; empty ⇒ no-op. |
| Remove Manual Override (new) | `DELETE /nutrition-data/{date}/override` | soft-deletes only the manual entry; returns the day (total falls back to the foods). Idempotent; 404 if no row. |
| Mark Day Fasting (new) | `PUT /nutrition-data/{date}/fasting` `{isFasting}` | only the fasting flag; turning it on with entries present ⇒ 409; not for future dates. |
| Mark Day Incomplete (new) | `PUT /nutrition-data/{date}/incomplete` `{isIncomplete}` | only the incomplete flag, only for a day that has data; never creates a day. |

No handler hard-deletes a `FoodLog` row any more (only account deletion cascades from `Users`). Code: `NutritionDataSupport.cs` (shared rules), the handlers under `Features/NutritionData/*` and `FoodLogs/{QuickEdit,ClearDayLog}`.
Concurrency and transactions audited: every operation is one `SaveChanges` (atomic) followed by the derived rebuild; `FoodLog` and entries carry `RowVersion`, so a concurrent loser gets 409 `CONCURRENT`, not 500; a failed `SaveChanges` leaves everything unchanged (tests 11, 12).

**Tests** (`tests/NdmSafetyRepro`, real handlers, temporary LocalDB, MARS on, audit interceptor like prod): 19 checks. On the pushed commit **10 of the 16 checks that exist there FAIL** (B1: `live 1, deleted 10`; B2: row/water/notes/flags gone); on the fixed code **ALL 19 PASS**: Quick Edit with 10 foods (all 10 remain), in-place edits, legacy multiple-QuickAdd day blocked, Clear keeps notes/override/water/flags, Delete NDM scope, Remove Override fallback, Fasting/Incomplete isolation, historical day (400 days back) with another day untouched, two concurrent Clear / Quick Edit / Clear+Save, failed transaction, aggregate freshness after Clear.

### Fully fixed vs only protected until S29
| Item | Status |
|---|---|
| B1 deleting foods on Quick Edit / NDM Save | **Fully fixed** (cannot happen any more). |
| B2 hard delete of the day row, water, notes, flags | **Fully fixed.** |
| Four intents share one handler | **Fixed** (separate endpoints and handlers). |
| Manual 2200 **over** Food Log 1800 (B3) | **Protected only**: the unsafe path returns 409 instead of replacing. The override itself needs the S29 columns. |
| Food added while a manual entry exists is summed (B3b: 2200 + 300 ⇒ 2500) | **NOT fixed** (needs the resolver + columns; Decision 3). Existing legacy behaviour, unchanged by P0. |
| Calories-only stores macros as 0 (B5) | **NOT fixed** (needs the nullable columns). |
| Stale `IsIncomplete` after the last entry is removed (B8) and Delete NDM keeping the incomplete flag | Open, reported as a business transition (U5). |
| Blocked days: the user cannot type a manual total over a day that has foods until S29 | Intended protection; FE must show the 409 message. |

## 3. Can a FoodLogs day row exist without entries? Yes
Paths: water-only day (`AddWater` uses `GetOrCreateAsync`), fasting day (`SetDayFasting`/NDM Save), the last food deleted (row stays, totals 0), flag-only (stale `IsIncomplete`), and now after Clear / Remove Override / Delete NDM (rows are kept). Consequence for S29: the day list and `LoadDaysAsync` must define "logged" as `Entries.Any() || IsFasting || ManualCaloriesKcal != null` (today `Entries.Any() || IsFasting`); a row's existence never means "logged".

## 4. Data-preservation strategy
1. Never delete: every removal is a soft delete of entries; no `FoodLog` row delete anywhere; no cleanup of legacy rows.
2. S29 is additive and nullable; legacy Quick Add entries are left exactly as they are (no auto-conversion).
3. The resolver reads the manual columns first and falls back to the entries as today, so un-migrated days behave as before (the legacy QuickAdd entry keeps counting as an entry until the user edits it).
4. Reconciliation of legacy rows is a separate, opt-in step (section 5), report first.

## 5. Legacy Quick Add audit (design; the SQL below is READ-ONLY and is NOT run on shared/prod without approval)
**What the code history proves** (`git log -S`): `FoodLogEntry.Source = QuickAdd` has been written only by `QuickEditDay` (since 2026-10-18, commit `8269afe`) and `SaveNutritionData` (since 2026-10-21, `b21c388`). The user-facing Quick Add (`POST /food-logs/quick-add`) never sets `Source`, so its entries are `Search`. Both manual writers create a new `FoodItems` row (`Source = QuickAdd`, `CreatedByUserId`; name "Manual entry" for NDM, user text or "Quick add" for Quick Edit).
**Classification rule (no assumption that every QuickAdd is an override):**
* A. `Entry.Source = 'QuickAdd'` and it is the only live entry of its day: **probable manual daily entry** (written by Quick Edit/NDM).
* B. `Entry.Source = 'QuickAdd'` and the day also has live non-QuickAdd entries: **mixed day** (manual written, then foods added): do NOT convert; report.
* C. several live QuickAdd entries on the day: report.
* D. `Entry.Source <> 'QuickAdd'` and the food is `Source = QuickAdd`: a genuine user Quick Add: never touched.
* E. soft-deleted entries (`IsDeleted = 1`) on a day that also has a QuickAdd entry created in the same transaction (`DeletedAt` ≈ the QuickAdd entry's `CreatedAt`): **foods lost to B1** before this fix; candidates for a **restore proposal** (never automatic).
```sql
-- read-only audit; counts only, no PII. Run on LocalDB/test copies; on prod only with explicit approval.
SELECT COUNT(*) AS TotalLogs, SUM(CASE WHEN NOT EXISTS (SELECT 1 FROM FoodLogEntries e WHERE e.FoodLogId=l.Id AND e.IsDeleted=0) THEN 1 ELSE 0 END) AS LogsWithoutLiveEntries FROM FoodLogs l;
SELECT e.Source, e.IsDeleted, COUNT(*) FROM FoodLogEntries e GROUP BY e.Source, e.IsDeleted;
WITH d AS (SELECT l.Id, SUM(CASE WHEN e.IsDeleted=0 THEN 1 ELSE 0 END) Live, SUM(CASE WHEN e.IsDeleted=0 AND e.Source='QuickAdd' THEN 1 ELSE 0 END) LiveQA,
                  SUM(CASE WHEN e.IsDeleted=1 THEN 1 ELSE 0 END) Dead FROM FoodLogs l JOIN FoodLogEntries e ON e.FoodLogId=l.Id GROUP BY l.Id)
SELECT SUM(CASE WHEN LiveQA=1 AND Live=1 THEN 1 ELSE 0 END) AS A_manualOnly, SUM(CASE WHEN LiveQA>=1 AND Live>LiveQA THEN 1 ELSE 0 END) AS B_mixed,
       SUM(CASE WHEN LiveQA>1 AND Live=LiveQA THEN 1 ELSE 0 END) AS C_severalManual, SUM(CASE WHEN LiveQA>=1 AND Dead>0 THEN 1 ELSE 0 END) AS E_deadFoodsOnManualDay FROM d;
```
Reconciliation options (report first, the user chooses): (1) leave everything (recommended default; the resolver treats class A as an entry until edited); (2) per-day opt-in "convert to manual columns" from the NDM screen (copies the entry values into the four columns, then soft-deletes that entry, one transaction, reversible by restoring the entry); (3) a restore proposal for class E days (un-delete the soft-deleted foods, put the QuickAdd entry's value into the manual columns), only as a reviewed script.

## 6. S29 design (for approval; NOT created, NOT applied)
**Columns on `FoodLogs`** (all `decimal(8,2) NULL`, no default, no backfill): `ManualCaloriesKcal`, `ManualProteinG`, `ManualCarbsG`, `ManualFatG`. Same precision as `Total*`. null = not entered, 0 = entered zero. Override present ⇔ `ManualCaloriesKcal IS NOT NULL` (calories are mandatory, see 8.2). No FoodItem and no FoodLogEntry for manual totals.
**Migration `S29_ManualNutritionColumns`:** `Up`: 4 × `AddColumn<decimal>(nullable: true)`. `Down`: 4 × `DropColumn` (safe: nothing else reads them before the resolver ships; the only loss is manual values entered after S29, and the P0 code still works without the columns, so rollback = redeploy the previous code and drop).
**Rollout order:** (1) approve the design; (2) generate S29 locally and verify `has-pending-model-changes` clean; (3) apply on a restored copy / LocalDB, run pre/post validation and the full test suite; (4) separate approval to apply on the shared DB (same route as S28); (5) deploy the code that reads the columns; (6) FE.
**Pre-migration validation (read-only):** `SELECT COUNT(*) FROM FoodLogs; SELECT COUNT(*), SUM(CAST(IsDeleted AS int)) FROM FoodLogEntries; SELECT CHECKSUM_AGG(CHECKSUM(Id,TotalCalories,IsFasting,IsIncomplete)) FROM FoodLogs;` and the last `__EFMigrationsHistory` row = S28.
**Post-migration validation:** same counts and checksum; `SELECT COUNT(*) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL` = 0; `INFORMATION_SCHEMA.COLUMNS` shows 4 nullable decimal(8,2); app smoke (`/health`, `GET /nutrition-data`).
**Backward compatibility:** old code ignores the columns; new code with NULL columns behaves exactly like today.

## 7. Resolver (Decision 3: OVERRIDE WINS) and consumers
`FoodLogs.Total*` stays the RAW Food Log total (sum of live entries). Effective intake is computed in ONE place, `FoodLogSupport.LoadDaysAsync` → `LoggedDay` (already used by Expenditure, Energy Balance, Macros, Nutrient detail, Check-in, Week log, Insights): `LoggedDay` gains `Source` (`Manual | FoodLog | Fasting | Imported`), effective `Calories/Protein/Carbs/Fat` (nullable), `FoodLogCalories/Protein/Carbs/Fat` (raw), `HasOverride`, and `AlgorithmKcal` (existing rule unchanged).
Priority: Manual → Food Log → (Imported: not built) → Fasting → Missing. Effective field = the manual value if the override exists (null stays null, never mixed with Food Log), else the food sum. Food mutations never write the manual columns; `RecalcTotals` keeps writing raw totals only. Example: Food Log 1800, override 2200, add 300 ⇒ raw 2100, effective 2200; Remove Override ⇒ effective 2100.
Must switch to effective: `GetNutritionData`, `GetDayLog` (+ raw totals), `NutritionOverview` (macros from the funnel, micros from foods, per-field denominators), `NutritionAggregates` (micros stay raw-food), Daily Progress snapshot (store effective), Macro series (null macros ⇒ no P/F/C/Other drawn for that day, calories kept), Energy Balance and Expenditure (already via `AlgorithmKcal`, now fed the effective kcal), Check-in. Averages: P/F/C use only days where that field is known (per-field denominator); kcal uses `AlgorithmKcal`.

## 8. Decisions I need (proposals only; partial-override semantics are NOT decided)
| # | Case | Proposal (recommended first) | Alternative |
|---|---|---|---|
| 8.1 | Calories-only manual | **Decided by the user**: macros null, effective macros null even if the Food Log has macros (no mixing); raw Food Log macros exposed separately. Stacked macro chart for that day: UNKNOWN (UX reference needed); keep calories, draw nothing invented. | |
| 8.2 | Macros-only (no calories) | Reject (400): calories are required for an override (as the validator does today). | Allow with kcal null ⇒ effective kcal falls back to Food Log (per-field mixing, contradicts "no merging of sources without an approved rule"). |
| 8.3 | Manual 0 kcal (not fasting) | Keep today's rule: 0 kcal without fasting is "not a valid kcal day" (shown as 0 in NDM, excluded from the algorithms); Fasting is how to say "ate nothing". | Treat an explicit manual 0 as a valid 0-kcal day. |
| 8.4 | Manual field null + Food Log value | Effective field null; the raw Food Log value is shown only in the "Food Log" breakdown. | Fall back per field to Food Log (mixing). |
| 8.5 | Clear all manual values | Not via PUT (a PUT needs calories); only `DELETE /nutrition-data/{date}/override` sets all four to NULL. | PUT with all fields null = remove. |
| 8.6a | Fasting + manual > 0 | Both directions 409 (as implemented now for fasting-with-entries); the user removes one first. | Manual save clears fasting. |
| 8.6b | Add food on a fasting day | Today `AddEntryAsync` clears fasting (`FoodLogSupport.cs:82`): keep. Fasting never coexists with raw foods; with an override the day is Manual (fasting flag off). | |
| 8.6c | Incomplete with override | Incomplete excludes the day from the algorithms whatever the source; Remove Override keeps the flag. | |
| 8.6d | Incomplete → complete | explicit user action only (`PUT /incomplete {false}`); never automatic. Stale flag after the last entry/override is removed (B8): clear automatically only if you approve. | |
| 8.6e | Clear Food Log after fasting | flags kept (implemented). | |

## 9. API contract v2 (additive; goes live with S29)
`GET /nutrition-data` item: `date, calories?, proteinG?, fatG?, carbsG?` (effective, nullable), `source` (`Manual|FoodLog|Fasting|Imported`, legacy `App` kept as an alias during the transition), `hasOverride`, `foodLogCalories, foodLogProteinG?, foodLogCarbsG?, foodLogFatG?` (raw), `isValidForAlgorithms`, `isFasting`, `isIncomplete`, `entryCount`. Days with no data are absent (FE shows Unlogged, never 0).
`PUT /nutrition-data[/{date}]` `{calories, proteinG?, fatG?, carbsG?}`: creates/updates the override, allowed on days with foods; null ≠ 0; equal to the current effective values ⇒ no write; response = the effective day.
`DELETE /nutrition-data/{date}/override`, `PUT /nutrition-data/{date}/fasting|incomplete` (already live, P0). `DELETE /food-logs/{date}` = Clear Food Log Day (live, P0). `GET /food-logs/{date}`: totals = effective + `foodLogTotals`, `source`, `hasOverride`.
List paging: default window 90 days, `limit` (120, max 366), `before` cursor, `hasMore`/`nextBefore`.
Freshness unchanged: `X-Derived-Data-State` after every mutation.

## 10. Tests after S29 (BE-testable rows of the spec matrix)
Calories-only / protein-only / fat-only / carbs-only / partial; null vs explicit 0; override with an existing Food Log; add/edit/delete food while an override exists (effective stays, raw changes); remove-override fallback (1800 → 2100 example); legacy QuickAdd preserved and untouched; average denominators with missing macros; calories chart keeps 2200; macro chart invents nothing; Expenditure/Energy Balance get the effective kcal (parity between NDM, Overview, Macros, EB); concurrent mutations; no deleted entries ever.
