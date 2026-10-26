# Expenditure: partial logging (complete / incomplete / fasting / unlogged) — audit + proposal

User decision 2026-10-26 (Decisions_2026-10-26_round2_BE_Done.md §2): keep `FreezeTdeeDuringCalibration` for now; fix the real cause first (partial logging). Rules: distinguish complete / incomplete / fasting / unlogged; the user confirms an incomplete day; incomplete days are excluded from learned TDEE by an approved rule; NEVER mark a day incomplete from a calorie threshold; NEVER auto-fill calories; re-verify Learn Day 1 / Day 7 on cleaned inputs. No estimator change, no snapshot rebuild. **This document is audit + proposal; the only code added is tests.**

## 1. What the code already does (read 2026-10-26)

Single source of truth: `FoodLogSupport.LoggedDay` (`AlgorithmKcal`, `IsValidForAlgorithms`), used by the rebuilder, series, insights, check-in and Flux inputs.

| Day class | How it is represented | Counts in Expenditure? |
|---|---|---|
| **Complete (valid)** | effective kcal > 0 (food log total, or Manual Nutrition override from S29) and `IsIncomplete = false` | yes (`AlgorithmKcal = kcal`) |
| **Incomplete** | user flag `FoodLog.IsIncomplete`, set only by the user via `PUT /nutrition-data/{date}/incomplete` (only on a day that has something logged; else 400 `IncompleteNeedsLoggedDay`); cleared automatically only when the day becomes truly empty (R2-4 `ClearStaleIncomplete`) | **no** (`AlgorithmKcal = null`): the day is not a valid log day, so it cannot enter intake averages nor the 4/7 rule |
| **Fasting** | user flag `IsFasting` (`PUT .../fasting`), only on a day with no food and no manual > 0 | yes, as a VALID 0 kcal day (MF help 16/74) |
| **Unlogged** | no entries and no manual value (kcal null), or 0 kcal without fasting (e.g. entries that sum to 0, manual 0) | no, ignored (MF help 74: a 0 kcal day without fasting is ignored) |

Tests that already pin this: `tests/AlgorithmChecks` (EffectiveDailyNutrition / LoggedDay cases), `ExpenditureHoldingRepro` scenario G (every day Incomplete ⇒ `logged7 = 0`, `MissingLogs`), `NdmSafetyRepro` (fasting/incomplete conflicts).

### 1.1 A confirmation flow already exists: the check-in "Partial Logging" module

`CheckInSupport.DayModel` marks a day `suspectedIncomplete` when it has a log with kcal > 0 and kcal < `AlgorithmOptions.IncompleteDayKcalRatio` (0.5) × that day's TDEE. This is a **display-only hint**: it changes no flag and no calculation. The module is `Required` when any of the last 7 days is suspected or already marked, otherwise `Skippable` (hidden if the user turned it off via `PUT /check-ins/modules`). The user confirms with `SetIncompleteDays` (`PUT /check-ins/incomplete-days`; dates only within the last 7 days, only on days with entries), which sets `IsIncomplete` and triggers `DerivedDataSync` (cause `DayStatusChanged`). It is the same user-confirmed mechanism as `PUT /nutrition-data/{date}/incomplete`. So both "the user confirms" and "never auto-mark" are already how it works; the threshold only decides which days are SUGGESTED.

## 2. The gap seen on the user's real account (2026-10-26)

10/20, 10/21, 10/22 hold ONE live entry each (233 / 64.5 / 232.5 kcal), each far below half of the 2381 TDEE, so the check-in module would have suggested all three. But: (a) the module only appears during the weekly check-in (the user's first check-in was not due yet or not done), (b) it only reaches back 7 days, (c) outside check-in no screen asks "is this day finished?". Until the user confirms, those days are valid (kcal > 0, not flagged) and count. The what-if showed Expenditure only starts updating because these three days count as valid logs (4/7 rule satisfied by tiny days).

## 3. Proposal (nothing implemented; each option needs approval)

**Rule that stays fixed:** a day becomes Incomplete only when the user says so. No calorie threshold flips a day. No calorie is ever auto-filled.

| Option | What it is | Schema | Effort |
|---|---|---|---|
| **A (recommended first)** | Surface the EXISTING suggestion earlier and outside check-in: the FE asks "Is this day complete?" for a past day that is suspected (or on opening Expenditure while it is Calibrating/Holding because of few valid logs), using the existing `PUT /nutrition-data/{date}/incomplete`. No new rule, no BE change. | none | FE |
| **B** | BE read-only hint in `/expenditure/insights`: `suspectedIncompleteDays: [dates]` (same `IncompleteDayKcalRatio` rule, but over the whole current learning window instead of 7 days). Display-only, so the FE prompt in A knows which days to ask about; never changes any flag or calculation. The threshold is the already-approved check-in one; any new threshold needs approval. | none | BE contract addition |
| **C** | New explicit state "user confirmed complete" so unconfirmed days could be treated differently (e.g. not counted until confirmed). Changes algorithm eligibility ⇒ business rule + migration. | migration | not proposed now |

Excluding incomplete days from learned TDEE: already implemented (§1, `AlgorithmKcal = null`). Options A/B only make the confirmation reachable sooner.

## 4. Re-check of Learn Day 1 / Day 7 on cleaned inputs (isolated LocalDB, real handlers, `tests/ExpenditureWhatIf`)

Inputs: the same 8 weigh-ins and 5 log days as the user's account (numbers reported in chat; the harness reads them from scratch files, nothing personal is stored in the repo). "Cleaned" = the 3 tiny days are marked Incomplete through the real handler (as a user would). Day 7 = `CalibratingDays = 7` with the freeze kept.

| Scenario | 10/22 | 10/24 | 10/25 | 10/26 |
|---|---|---|---|---|
| Freeze 14 days (production now) | 2381 Calibrating | 2381 | 2381 | 2381 Calibrating |
| Learn Day 1, data as is | 2208 Updating | 2208 Holding | 2097 Updating | **2033 Updating** |
| Learn Day 1, tiny days marked Incomplete | 2381 Calibrating | 2381 | 2381 | **2381 Calibrating** (valid log days 1/7 then 2/7) |
| Day 7 freeze, data as is | 2381 Calibrating | 2381 | 2252 Updating | **2173 Updating** |
| Day 7 freeze, tiny days marked Incomplete | 2381 Calibrating | 2381 | 2381 | **2381 Calibrating** |

Reading: once the input is cleaned, neither Day 1 nor Day 7 produces a number by 10/26, because only 10/25 and 10/26 are valid log days (needs 4/7, or 3 in a row). The apparent "learned" values (2033 / 2173) come from the three near-empty days. So input quality, not the start day, drives the result. A start day (1, 7 or 14) is a second-order decision; picking one now would not change what the user sees once they confirm those days are incomplete.

## 5. Needs the user

1. Which option (A, A+B, C) for confirming incomplete days.
2. Whether Day 1 / Day 7 should be revisited after the user has had a chance to confirm incomplete days (the freeze stays meanwhile).
3. Wording is MacroOS UX, not MF: MF help 16 only defines fasting and 74 says 0 kcal days are ignored; MF's treatment of partial days is UNKNOWN.
