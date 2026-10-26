# Expenditure: resume after Holding, split by cause — transition table + regression tests (before any state-machine change)

User decision 2026-10-26 (round 2 §3): split the two causes. Weight missing → research/implement "one new weigh-in" (MF Article 109) when nutrition still qualifies, and check that the raw calculator can actually compute. Nutrition insufficient → keep the approved policy until enough data to verify. Do not merge the two causes into one hard rule. **Present the transition table and regression tests BEFORE touching the production state machine** — this is that document. No production code is changed by it; the new tests only pin CURRENT behaviour.

## 1. Result of the audit (short)

1. **Weight-missing Holding already resumes after ONE new weigh-in when nutrition qualifies.** `ExpenditureStatusMachine.Next`: `Holding/Calibrating → Updating` when `CanMaintain (W7 ≥ 1 ∧ L7 ≥ 4) ∨ CanResume (W7 ≥ 1 ∧ Run ≥ 3)`. With a weight gap and logs 4+/7, a new weigh-in makes `W7 = 1` ⇒ `CanMaintain` ⇒ Updating, no extra "3 days". Pinned by `StatusMachine §3` checks and repro **N1** (TDEE moves from 2381 to 2398 the same day).
2. The `3` (`ResumeConsecutiveDays`) applies ONLY to the nutrition cause (`L7 < 4`, `Run ≥ 3`, plus a weigh-in). It is a MacroOS policy: MF help 110 gives no number (UNKNOWN); the old "VERIFIED" comment was wrong and is corrected.
3. **The real gap is the raw calculator, not the status machine.** After the machine says Updating, `ExpenditureCalculator.ComputeRaw` needs ≥ 2 trend points inside the 21-day window (counted from the program start) and ≥ 1 valid log day between them. If the only other weigh-in is older than 21 days, raw is null and the rebuilder turns Updating into Holding with reason `NeedsTwoWeighIns` (repro **N2**: weigh-ins 40/35 days ago + one today ⇒ Holding; add yesterday's ⇒ Updating). MF 109 says one new entry is enough, but MF's V3 internals are UNKNOWN, so "2 points" is a MacroOS requirement of its own formula (Δ trend needs two trend points).

## 2. Transition table (CURRENT, with the proposed delta marked)

Inputs per evaluation day d (window = last 7 calendar days ending d; "valid log" = `AlgorithmKcal` not null): `W7` = days with a trend weight, `L7` = valid log days, `Run` = consecutive valid log days ending at d (today pending excluded), `Raw` = `ComputeRaw` computable (≥ 2 trend points in 21 d from start, ≥ 1 valid log day between first and last).

| # | Previous | Cause of not being Updating | Condition | Next today | Reason code today | Proposed |
|---|---|---|---|---|---|---|
| 1 | Updating | — | `W7 ≥ 1 ∧ L7 ≥ 4 ∧ Raw` | Updating | — | no change |
| 2 | Updating | weight | `W7 = 0 ∧ L7 ≥ 4` | Holding | `NoRecentWeighIn` | no change |
| 3 | Updating | nutrition | `L7 < 4` (today pending ⇒ stays Updating) | Holding | `MissingLogs` | no change |
| 4 | Updating | both | `W7 = 0 ∧ L7 < 4` | Holding | `NoRecentWeighIn`, `MissingLogs` | no change |
| 5 | Updating | raw | `W7 ≥ 1 ∧ L7 ≥ 4 ∧ ¬Raw` | Holding (TDEE held) | `NeedsTwoWeighIns` / `NoLogsBetweenWeighIns` | see §3 (the only delta) |
| 6 | Holding / Calibrating | weight only | new weigh-in, `L7 ≥ 4` | **Updating immediately** (if Raw) | — | no change (already MF 109) |
| 7 | Holding / Calibrating | weight only, raw missing | new weigh-in, `L7 ≥ 4`, other weigh-in older than 21 d | Holding | `NeedsTwoWeighIns` | §3 option B2 |
| 8 | Holding / Calibrating | nutrition only | `W7 ≥ 1`, `L7 < 4`, `Run ≥ 3` | Updating (if Raw) | — | keep policy (`3`), UNKNOWN in MF |
| 9 | Holding / Calibrating | nutrition only | `W7 ≥ 1`, `L7 < 4`, `Run < 3` | stays | `MissingLogs` / `RecoveringLogs` | keep policy |
| 10 | Holding / Calibrating | both | `W7 = 0` | stays | `NoRecentWeighIn` (+ `MissingLogs`) | no change |
| 11 | any | initial calibration | first `CalibratingDays` from program start with `FreezeTdeeDuringCalibration` | Calibrating, TDEE frozen | `InitialCalibration` | unchanged (user kept the freeze) |

Status machine split (what "split the two causes" means in code, proposed): none needed for the weight cause (row 6 already holds). Keep rows 8–9 as the nutrition-only rule. Rows 2/4/10 keep the cause visible through separate reason codes, which the API already returns.

## 3. The one real delta: when raw cannot be computed after a long weight gap (rows 5 and 7)

This changes the estimator's input window, so it is a business/estimator rule and **needs the user's approval before any change**.

| Option | Behaviour | Risk |
|---|---|---|
| **B0 (current)** | needs two trend points in the 21-day window; reason `NeedsTwoWeighIns` tells the user to weigh again | none; matches the user's "do not drop the guard" |
| **B1** | keep as is but improve the message: when Holding was caused by a weight gap, say explicitly "one more weigh-in in the next N days" | wording only |
| **B2** | use the last stored trend point BEFORE the window as the start anchor (carry-in) when the window has only one point, only if the anchor is not older than a cap (e.g. 35 days) and valid log days exist between anchor and the new weigh-in. Lets ONE new weigh-in compute raw after a gap | longer span ⇒ more missing-log days inside the span (the calculator averages logged days only); the cap is a MacroOS assumption, MF value UNKNOWN |

No option touches nutrition-cause rules. Not proposed: dropping the 2-point requirement entirely (Δ trend is undefined with one point).

## 4. Regression test list

Already in the tree and passing (2026-10-26):
- `AlgorithmChecks` `StatusMachine` block: new chain = Calibrating; 3 consecutive + weigh-in ⇒ Updating; non-consecutive 3 ⇒ no resume; 4/7 + weigh-in ⇒ Updating; **missing weight ⇒ not Updating, one new weigh-in ⇒ Updating immediately (`LLLLw`, and the new `§3` check with a 10-day weight gap)**; Updating→Holding when `L7 < 4`; Holding resume with 3 consecutive; 2 consecutive then gap ⇒ no resume; today-pending keeps Updating; `§3` nutrition-cause check (resume only on the 3rd consecutive valid day + weigh-in).
- `ExpenditureHoldingRepro` (real handlers, isolated DB): A–M (reasons and codes), **N1 (one new weigh-in resumes at once, TDEE moves)**, **N2 (old weigh-ins outside the window ⇒ `NeedsTwoWeighIns`, second weigh-in ⇒ Updating)**.

To add only if B2 or any resume change is approved: (1) anchor older than the cap ⇒ still `NeedsTwoWeighIns`; (2) anchor inside the cap with logs between ⇒ Updating and TDEE computed from anchor→new point; (3) anchor with no valid log day between ⇒ `NoLogsBetweenWeighIns`; (4) B2 never changes rows 1–4, 8–10 (property test over random patterns comparing old/new `Next`); (5) a rebuild of existing snapshots is NOT triggered (version label changes only for rows actually recomputed); (6) freeze period still wins (row 11).

## 5. Needs the user

1. Confirm "the weight cause is already satisfied by the status machine" (row 6) so no code change is needed there.
2. Choose B0 / B1 / B2 for the long-gap case (B2 is an estimator-input change).
3. Keep `ResumeConsecutiveDays = 3` as the labelled MacroOS policy for the nutrition cause until MF data exists.
