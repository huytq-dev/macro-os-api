# Business Verification — open decisions (single table)

Source: `Docs/Design/Flux/F_Estimator_Study.md` §7 (15 rows). Standing state: estimator kept as is (user: "Giữ production như hiện tại"), Flux hidden (null), ON_TARGET classifier inactive, S28 not approved. Order = calculation correctness and historical integrity first. "Recommendation" is MOS-BE's suggestion only; every row needs the user's decision. MF behaviour is marked UNKNOWN where no MF source exists (no invention).

| P | # | Topic | Current behaviour | Options | Recommendation | Impact if decided |
|---|---|---|---|---|---|---|
| 1 | 15 | Latest-weight lookups / stale trend | latest-weight lookups ignore recency; a stale trend feeds Start and targets; fallback to raw weight when trend is null; start snapshots are not refreshed after history edits | (a) keep; (b) cap staleness (N days) and show "no recent weight"; (c) refresh start snapshots on history edit | (b) + (c) behind a flag, no rebuild of history | changes targets/Start for users with old weights; historical snapshots stay unless (c) approved; needs a bounded rebuild decision |
| 2 | 5 | D8: GET endpoints creating goals | `EnsureActiveGoalAsync` can create a goal inside GET handlers (write on read); duplicate active goals possible | F1 stop creation; F2 make two GETs read-only; F3 data backfill migration; F4 unique active-goal index | F1 + F2 now (no schema), F4 with S28 after a dedupe check; F3 only with explicit data approval | stops silent data creation; F3/F4 touch history and need a migration (not applied) |
| 3 | 11 | "Learn from day 1" (`FreezeTdeeDuringCalibration=false`, version "7") | off; 14-day Calibrating freeze (version "6") | enable globally / per user / keep | keep off until approved; a release needs a rebuild policy | new snapshots stamped "7"; `IsNewer` blocks overwrites by "6" → rollback must be roll-forward |
| 4 | 12 | Label hysteresis `SmoothLabels` (2-day rule) | off | enable / keep off | keep off; UX-only, no number changes | labels only |
| 5 | 8 | Scale vs SmartScale same day | two methods stored; merge rule open | two methods / merged (priority) / average | needs MF reference or user rule; keep current storage | changes which weight feeds trend → TDEE and goals; historical impact if merged retroactively |
| 6 | 9 | Steps average divisor | open | logged days / calendar days | logged days (consistent with Macros per logged day), pending user | display and any derived step goals |
| 7 | 7 | Macros averages denominator | per logged day; fasting counts; incomplete/missing excluded; MF UNKNOWN | logged only / calendar / exclude fasting | keep; get MF evidence before changing | displayed averages only; no stored data |
| 8 | 10 | Adherence for legacy (`isUnverified`) days | open display of legacy days | include / exclude / mark | exclude from the percentage, show separately | display only |
| 9 | 6 | Negative **Other** in macros chart | not drawn; note shown; percentages un-normalised; MF UNKNOWN | keep / draw / clamp | keep | FE only |
| 10 | 14 | Onboarding Prior Weight threshold (weight ÷ 0.9) | `MACROOS_PROVISIONAL_POLICY`, not MF-verified | keep provisional / change | keep, label as provisional | new-user onboarding only |
| 11 | 13 | ON_TARGET tolerance (δ, k, λ) | classifier off; proposal doc | adopt / adjust / drop | keep inactive until user fixes δ, k, λ | check-in decisions for stable users |
| 12 | 3 | Goal Tracking Q1–Q7 (picker, MuscleGain/Endurance/Flexibility audit, valid measurement, reached, dismiss, edited/deleted reaching weigh-in, trend completion) | direction for Endurance/Flexibility "pending user decision" (`GoalRules.cs:19-20,35-36`); no MF reference | answer each | decide Q3, Q4, Q6 first (they define "reached" and historical integrity) | needs S28 (not approved) |
| 13 | 4 | Goal Tracking Q8–Q15 (new Goal/Program while prompt open, progress after Complete, edit after Complete, target edit while reached, MF screenshots, achievement storage O1–O4, `StartWeightKg`, goal-only creation) | UNKNOWN | answer each | decide Q13/Q14 (storage, start weight) before any schema work | schema (S28) |
| 14 | 2 | Flux | hidden: API returns null for all days; research engine unwired; validation negative | keep hidden / revisit with evidence | keep hidden | none now |
| 15 | 1 | Estimator | **CLOSED** (production kept; Version B parked as flagged-off proposal, `F_Estimator_Study.md` §9) | — | — | — |

Decisions that need schema work are blocked by "S28 not approved": #2 (F4), #12, #13.
