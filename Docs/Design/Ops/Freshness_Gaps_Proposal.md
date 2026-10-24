# Freshness gaps G3, G5, G6, G8 — analysis and proposals (nothing implemented)

Status: proposal for the user's approval, 2026-10-23. Source: `Mutation_Dependency_Matrix.md` §3.3. Code reading plus the temporary-DB harness of `tests/DerivedDataRepro`; no API started, no prod access, no migration, no algorithm, estimator, Goal Tracking or ON_TARGET change. Paths under `BE/src`.

Already in place (context): P1 header `X-Derived-Data-State` on every successful mutation (`API/Filters/DerivedDataStateFilter.cs`), P5 user lock in Reset/SetManual (`Application/Features/DerivedData/DerivedDataLock.cs`), G1 fix in `ExpenditureRebuilder.cs`.

## 0. Summary and recommended order
| Gap | User-visible impact | Severity | Business rule? | API contract? | Needs approval |
|---|---|---|---|---|---|
| **G8** terminal `Failed` never clears | red "update failed" state after every later edit (now amplified: the P1 header says `Failed` on **every** mutation) until the user presses retry | medium-low | no | no (state precedence only) | no, small; listed for transparency |
| **G5** aggregates delayed when the sync is queued | nutrient overview / series show old numbers for seconds to minutes after logging food | low-medium | no | no | recommended: yes (touches write path) |
| **G6** check-in Accept can use a TDEE that is not recalculated yet | calorie targets for the next week computed from a stale TDEE | high | **yes** (Accept may be refused) | **yes** (additive fields + new 409 reason) | **yes — your lean is Accept blocked while not Ready** |
| **G3** source committed, job not enqueued on crash | silent, long-lived stale expenditure/daily progress; also a tiny race that weakens G6 | medium, low probability | no | no | proposal + regression strategy only, as you asked |

Order: **G8 → G5 → G3 (write-ahead job) → G6**. G6 can ship first as a gate, but without G3 it keeps a millisecond window (§4.5).

---
## 1. G8 — terminal `Failed` job is never cleared by a later successful rebuild
**Impact.** `GET /derived-data/status` returns `Failed` while any job has used all 5 attempts, even when a newer rebuild has since repaired the same days. With P1, every later mutation returns `X-Derived-Data-State: Failed`, so FE shows an error after each edit until the user presses "retry".

**Reproduction (deterministic, temp DB).** Seed `RecalculationJob { State = Failed, AttemptCount = 5, FromDate = d, ToDate = d }`; apply any food edit whose sync rebuild completes. Status stays `Failed`: `DerivedDataJobs.StatusAsync` (`Features/DerivedData/DerivedDataJobs.cs:76-96`) lists every non-Succeeded job and sets `failed = open.Any(Failed && attempts ≥ max)` (`:91`). The `DerivedDataRepro` P1 test already shows header `Failed` for this seed.

**Root cause.** (1) Nothing closes an exhausted job: the worker only claims Failed jobs with attempts left (`DerivedDataWorker.cs:54,82`) and `RunClaimedAsync` merges only Pending jobs (`DerivedDataJobs.cs:51-53`); only `POST /derived-data/retry` resets it. (2) `Failed` takes precedence over `Updating` (`:92`), so even a fresh Pending job being processed shows as Failed.

**Proposed fix (no contract, no schema).**
1. `StatusAsync`: `Failed` only when **no** job is Pending/Running/retryable and at least one exhausted job exists; otherwise `Updating`.
2. `RunClaimedAsync`: besides Pending jobs, also take the user's **exhausted Failed** jobs into the same run (`from = min(FromDate)` over all of them); on success they become Succeeded in the same transaction. A poison cause then fails the new job too, which keeps `Failed` honest.
3. Do **not** mark exhausted jobs Succeeded after a *bounded* sync rebuild (its convergence early-stop assumes stored rows are consistent, which is exactly what a failed job breaks).

**Tests.** `DerivedDataRepro`: (a) Failed(5) + new edit synced ⇒ still Failed until a worker run (`RunClaimedAsync`) ⇒ Ready and the job Succeeded; (b) Failed(5) + Pending ⇒ `Updating`; (c) a rebuild that throws ⇒ stays Failed, no data change; (d) retry endpoint behaviour unchanged. Pure: extract `DerivedDataStatusRules.Decide(jobs)` and table-test precedence.
**Business rule / contract:** none. Only the documented meaning of the three states is tightened.

---
## 2. G5 — aggregates are delayed whenever the synchronous rebuild is queued
**Impact.** After logging/deleting food, `/nutrition/overview` (long ranges), `/nutrients/{code}/series` and `/nutrients/{code}/day` can show the old day totals until the worker finishes (seconds; minutes if the worker is busy). Reads that go to the logs directly (food log day/week, macro series) are right immediately. The nutrient reads' self-heal only fills **missing** days (`Features/Nutrients/NutritionAggregates.cs:97-106`), it never notices a **stale** one.

**Reproduction (deterministic, temp DB).** Another session holds `DerivedDataSync.LockResource(userId)` (as the P5 test does) and the request lets `DerivedLockWaitMs` (3 s, `AlgorithmOptions.cs:16`) elapse: `DerivedDataSync.RunAsync` takes the `Queued` branch (`DerivedDataSync.cs:46-53`) and **never refreshes aggregates** (the refresh is at `:55`, after the lock). Read `/nutrients/{code}/series`: old number, status `Updating`. Same when the rebuild throws (`:69-77`). Real triggers: a long worker rebuild after a big weight edit, double-tap from two devices.

**Severity.** Low-medium: transient, but it is the only derived read that is wrong although the source is committed and the numbers are cheap to compute.

**Root cause.** The aggregate refresh sits inside the expenditure transaction/lock although it depends only on source rows and costs a handful of queries.

**Proposed fix (no schema, no contract).** In `FoodLogSupport.SaveAndRebuildAsync` (`Features/FoodLogs/FoodLogSupport.cs:174-181`) and `WaterSupport`, refresh the touched days' `NutritionDayAggregate` **before** `DerivedDataSync.RunAsync`, in its own short transaction under a **separate, short per-user lock** `macroos:agg:{userId}` (not the rebuild lock, so it never waits for a long rebuild). The separate lock is required: two refreshers that each read the source and write later could otherwise overwrite a newer total with an older one. A unique-index collision on a first insert is handled like `ExpenditureSettingsSupport.GetOrCreateAsync` (discard, re-read). The refresh inside `DerivedDataSync` stays (harmless, idempotent) or is removed once the new path is proven.
Optional read-side guard: flag a day stale when `LastCalculatedAt < max(entry.ModifiedAt)` and refresh it on read (only if the write-path fix is rejected; it makes GETs heavier).

**Tests.** `DerivedDataRepro`: (a) lock held by another session ⇒ edit returns Queued **and** the aggregate already equals the source; (b) two concurrent edits of one day ⇒ final aggregate equals the final source (run N times); (c) water add/delete unchanged; (d) rebuild exception ⇒ aggregate still fresh, job enqueued; (e) a failing aggregate refresh does not lose the source write (it must enqueue a job, see G3). Pure: none needed.
**Business rule / contract:** none. FE rule "nutrition screens are Derived" can be relaxed afterwards (they become Live).

---
## 3. G3 — source committed, derived job not enqueued (crash window)
**Impact.** The edit is saved but expenditure/daily progress/aggregates for that day are never rebuilt and status says `Ready`. The staleness is silent and long-lived: a later edit rebuilds only from **its** date forward, the nightly job covers only yesterday + today for users at local midnight (`ExpenditureNightlyJob.cs:22-42`), so earlier days stay wrong until someone presses retry or edits that day again.

**Reproduction.** Process death between the two steps of `SaveAndRebuildAsync`: `EnsureSaveAsync` commits the source (`FoodLogSupport.cs:177`), then `DerivedDataSync.RunAsync` has not yet enqueued. Deterministic test: save the source through the real handler path with a `UnitOfWork` wrapper that throws a non-catchable fault (or simply stops) before `RunAsync`; then `StatusAsync` ⇒ `Ready`, daily snapshot ⇒ old kcal. Real triggers: **WebDeploy takes the site offline with `app_offline.htm`** (`.github/workflows/deploy-monsterasp.yml`), which kills in-flight requests; IIS recycle; host restart; SQL outage exactly at the enqueue. Second form: if `DerivedDataJobs.EnqueueAsync` itself throws inside the `catch` block (`DerivedDataSync.cs:74`), the exception escapes after the source was saved: the client gets 500 although the edit exists, and no job exists.

**Severity.** Medium (silent, permanent until noticed), low probability (milliseconds per write; deploys are the realistic cause).

**Root cause.** Two transactions with no durable intent between them; the job table is the only recovery mechanism and it is written after the source.

**Options.**
| | A. Write-ahead job (recommended) | B. Reconciler | C. Do nothing |
|---|---|---|---|
| Idea | enqueue a Pending job in the **same** `SaveChanges` as the source; the sync rebuild then marks it Succeeded in its own transaction | an hourly worker pass finds users whose newest source `ModifiedAt` is newer than their newest derived write and enqueues from the earliest changed date | accept the window |
| Guarantee | none lost; also closes the in-request race of §4.5 | bounded delay (≤ 1 h), no per-write cost | none |
| Cost | +1 insert and +1 update per mutation (jobs deleted after 7 days, `DerivedDataWorker.cs:62`) | one scan query per hour; false positives because snapshots only change `ModifiedAt` when a value changes | none |
| Risk | coverage rule must be exact (below) | cannot tell "rebuilt, same values" from "never rebuilt" | silent staleness |

**Design of A.** `SaveAndRebuildAsync` / `WeightTrendRefresher.RebuildExpenditureAsync` / `SetIncompleteDays` call `DerivedDataJobs.EnqueueAsync` **instead of** `EnsureSaveAsync` (it already runs `SaveChangesAsync` over everything tracked, `DerivedDataJobs.cs:35`), obtaining the job id and whether it **created** the job or merged into an existing Pending one. `DerivedDataSync.RunAsync` gets that id: on `ResumeFrom == null` and a job **created by this request**, mark it Succeeded inside the rebuild transaction; if the job was merged into an older Pending one, leave it Pending (the worker finishes; status `Updating`). Do **not** call `Wake()` on the write-ahead enqueue (the request is about to rebuild). If the worker polls first it waits for the lock and repeats the rebuild: wasted work, not wrong data. Failure of the enqueue ⇒ the whole write rolls back (no half state). The idempotent retry (`IdempotentWrite`) re-runs the handler and would enqueue again ⇒ job merging makes that idempotent.

**Regression strategy** (fault injection with the real handlers on the temp DB):
1. Kill after the source commit (wrapper aborts) ⇒ a Pending job exists, status `Updating`, the worker run repairs daily progress/expenditure/aggregates ⇒ `Ready`.
2. Happy path ⇒ no Pending job remains, status `Ready` at response, snapshots equal a from-scratch rebuild.
3. Merge with an existing Pending job ⇒ one job, union of dates, job stays Pending after the sync.
4. Source save fails ⇒ no job row.
5. Two concurrent requests, same user ⇒ final snapshots equal a full rebuild; no lost job.
6. `IdempotentWrite` retry ⇒ still one job.
7. Old build still running on MonsterASP processes the new Pending jobs correctly (backward compatible) and a rollback leaves processable jobs.
8. Row-budget: jobs per 1000 mutations and table size after 7-day cleanup stay bounded.
9. Existing suites: AlgorithmChecks, DerivedDataRepro, ConcurrencyRepro.
**Business rule / contract:** none; one extra `Updating` flicker is possible only for a concurrent status poll during the request.

---
## 4. G6 — check-in Accept may use a TDEE that is not recalculated yet
> **IMPLEMENTED (BE) 2026-10-23 on the user's approval.** `Features/CheckIns/CheckInGate.cs` (pure rule), `CheckInSupport.DecideAsync` (Accept runs under `DerivedDataLock`, reads status inside the lock, 409 + `errors.reasonCode` = `DERIVED_DATA_UPDATING` / `DERIVED_DATA_FAILED`, nothing written; Reject and the "already decided today" branch unchanged), `CheckInPreviewResponse.DerivedDataState` + `AcceptBlockedReason` (additive, default `Ready`/null). Tests: `AlgorithmChecks` gate table; `DerivedDataRepro` G6 scenarios on the temp DB with the real `CheckInSupport` (Updating, Failed, Reject-while-Failed, Ready accept, idempotent repeat, preview fields, lock-held ⇒ busy 409). Remaining window of §4.5 stays until G3-A. FE UX table of §4.4 approved for MOS-FE.
>
> **Officially approved (user, 2026-10-23) and verified.** Final contract:
> - `POST /api/check-ins/accept` → **409 Conflict** (`ConflictException`, `Contract/Exceptions/ConflictException.cs:3-4`) with body `errors.reasonCode = ["DERIVED_DATA_UPDATING"]` (a job is Pending/Running/retryable, **or** a rebuild holds the user lock longer than `DerivedLockWaitMs`) or `["DERIVED_DATA_FAILED"]` (a job used all attempts; FE offers `POST /api/derived-data/retry`). `message` = `ExceptionMessages.CheckInDerivedDataNotReady`. Nothing is written.
> - `GET /api/check-ins/preview` and `GET /api/check-ins/session` (`preview` object): `derivedDataState` = `"Ready" | "Updating" | "Failed"`, `acceptBlockedReason` = `null | "DERIVED_DATA_UPDATING" | "DERIVED_DATA_FAILED"`.
> - `POST /api/check-ins/reject` unchanged in every state; an Accept already recorded today still returns its id (idempotent).
> - Ordering inside Accept (`CheckInSupport.DecideAsync`): idempotent "already decided today" check → `BEGIN TRAN` → `sp_getapplock` on `DerivedDataSync.LockResource(user)` (same lock as every rebuild, wait ≤ 3 s) → **repeat the "already decided today" check under the lock** (a parallel Accept from another tab may have committed while this one waited: same decision ⇒ its Id, different ⇒ 409 `CheckInDecisionConflict`; added after the independent review, test `DerivedDataRepro` 9a-9c) → read job state (`OpenStateAsync`). Races with an unlocked Reject (two tabs) are resolved after the fact: a unique-index loss (`DbUpdateException`, loser fully rolled back) re-reads today's decision ⇒ its Id or 409 `CheckInDecisionConflict`; a build that sees a decision already recorded today returns the same idempotent result instead of `CheckInNotDue` (test 9c′, Matrix G9) → build context (TDEE, trend, targets) → write week + check-in → `COMMIT`. A rebuild that is running when Accept arrives finishes first; Accept then sees its result or its new job.
> - Integration tests (`tests/DerivedDataRepro` 8a-8g, temporary LocalDB, real `CheckInSupport`, `RetryDerivedDataCommandHandler`, `DerivedDataJobs.RunClaimedAsync`): Ready 200 (fingerprint changes: positive control); Updating 409 with every table unchanged (programs, program weeks, week day targets, day targets, expenditure + daily snapshots, check-in history, goals: `COUNT` + `CHECKSUM_AGG(BINARY_CHECKSUM(*))`); Failed 409 unchanged → Retry ⇒ Updating (still 409, no check-in) → worker run ⇒ Ready ⇒ Accept 200; race: a job created while Accept waits for the lock ⇒ 409, unchanged; race: a rebuild that finishes while Accept waits ⇒ Accept 200 storing the NEW TDEE; lock held past the wait ⇒ 409 `DERIVED_DATA_UPDATING`, unchanged; preview fields.
**Your lean (relayed):** "không cho Accept Check-in sử dụng TDEE chưa được tính lại xong". The proposal below follows it; it needs your approval before any code.

### 4.1 Impact
`CheckInSupport.BuildAsync` (`Features/CheckIns/CheckInSupport.cs:22-26,58-59`) takes the **latest stored** `ExpenditureSnapshot` and builds the proposal from its TDEE and status. While `Updating`/`Failed` that snapshot predates the user's latest logs/weights, so the proposed calories (and whether a proposal exists at all: `hasProposal = tdee.Status == Updating`) can be wrong; `CommitAsync` (`:151-184`) then writes the new week and stores that TDEE in `WeeklyCheckIn.TdeeEstimate`. Preview/session show the same stale proposal. Nothing in the check-in code reads the derived status (the only consumer is `InitialTdeeResolver.cs:43`).

### 4.2 Reproduction (temp DB)
Active coached program, check-in due, snapshots through today. Edit a day's log so the TDEE would change, then hold the user lock (or leave a Pending job) so the rebuild is queued. `POST /check-ins/accept` succeeds today and stores the old TDEE and targets.

### 4.3 Severity
High: it changes the user's weekly calorie/macro targets from data the system itself knows are outdated.

### 4.4 Proposed behaviour
**BE validation (business rule + API behaviour change; needs approval).**
- `CheckInSupport.DecideAsync` with `accepted: true`: run the whole decision under `DerivedDataLock.RunAsync` (already implemented for P5), then **inside the lock** read `DerivedDataJobs.StatusAsync`. State `Updating` ⇒ `ConflictException(409)` with `errors: { reasonCode: ["DERIVED_DATA_UPDATING"] }`; `Failed` ⇒ 409 `DERIVED_DATA_FAILED`; lock not obtained in 3 s ⇒ the existing `ExpenditureSettingsBusy` 409. Only then build the context and commit. Nothing is written when refused (no week, no `WeeklyCheckIn`).
- **Reject stays allowed** in every state: it copies the previously accepted targets and uses no TDEE (`CommitAsync:156-159`).
- The idempotent "already decided today" branch (`DecideAsync:135-140`) stays first, so a double tap after a successful Accept still returns the old id even if a job is Pending later.
- Preview/session (GET) are not refused; they stay readable.

**API contract (additive, needs approval).** On `CheckInPreviewResponse` (`Contract/Models/CheckIns/CheckInPreviewResponse.cs`; session embeds it): `derivedDataState` (`Ready | Updating | Failed`) and `acceptBlockedReason` (`null | "DERIVED_DATA_UPDATING" | "DERIVED_DATA_FAILED"`). Old clients ignore them; the 409 protects them anyway. `ReasonCodes` unchanged.

**UX proposal (FE, for approval; wording is a suggestion).**
| State | Accept button | Body |
|---|---|---|
| Ready | enabled | as today |
| Updating | **disabled**, small spinner | banner "Your expenditure is being recalculated from your latest logs. Accept unlocks when it finishes." The proposal card stays visible but is labelled "may change". FE polls with the rules of `Mutation_Dependency_Matrix.md` §4 (1 s, 2 s, 3 s, then 5 s, max 3 min); on `Ready` **refetch the preview** and, if numbers changed, show a one-line "Updated" note before enabling Accept |
| Failed | disabled | banner "We couldn't update your expenditure." + **Retry** (`POST /derived-data/retry`) then the Updating behaviour |
| any | **Reject stays enabled** | unchanged |
A user who is stuck in `Failed` can still Reject (targets unchanged) and retry; no data is lost.

### 4.5 Remaining window and why G3-A matters
Between a mutation's source commit and its job enqueue/lock (milliseconds), `StatusAsync` reports `Ready` while the rebuild has not started, so a gate based only on the status can still pass. The write-ahead job (G3-A) makes the Pending job exist from the source commit, closing that window. Without G3-A the gate removes the realistic case (queued/failed/running rebuild) and keeps this narrow race. A timestamp comparison (source `ModifiedAt` vs snapshot) is **not** a safe substitute: snapshots only change `ModifiedAt` when a value changes (`ExpenditureRebuilder.cs:136-152`), so a correct rebuild with identical numbers would look stale.
**Confirmed by the independent review (2026-10-23), left as the known G3 residual:** the window exists on every path where the source commits before any job row exists: the normal sync (`UpsertWeightEntryCommandHandler.cs:60` commit → `:65` → `DerivedDataSync.cs:45-46` lock; `FoodLogSupport.cs:177` save → `:180` sync), the budget-0 path (`DerivedDataSync.cs:35-41`, enqueue right after the source commit), and the failure path, where `DerivedDataSync.cs:72` rolls back, which releases the lock, before `:74` enqueues, so an Accept waiting on the lock can be granted it and still read `Ready`. All of them close with G3-A (job written in the same `SaveChanges` as the source, left Pending on failure/timeout, marked Succeeded inside the locked rebuild).

### 4.6 Tests
Pure: `CheckInGate.Evaluate(state, accepted)` table (Ready/Updating/Failed × Accept/Reject). `DerivedDataRepro` (real handlers, temp DB): Pending job ⇒ Accept 409 `DERIVED_DATA_UPDATING`, no `WeeklyCheckIn` row, no new `MacroProgramWeek`; exhausted Failed ⇒ 409 `DERIVED_DATA_FAILED`; Ready ⇒ Accept succeeds; Reject succeeds in all three; double Accept after success ⇒ same id; lock held by another session ⇒ 409 busy; after the job is Succeeded ⇒ Accept succeeds and `TdeeEstimate` equals the rebuilt snapshot; preview carries the two new fields. Existing check-in vectors in AlgorithmChecks stay green.
**Business rule:** yes (Accept refused while derived data is not Ready). **Contract:** yes, additive fields + a new 409 reason.

---
## 5. Decisions needed
1. G6: approve the BE gate (409 reasons, Reject allowed) and the additive preview fields; approve the FE UX table.
2. G3: choose A (write-ahead job), B (reconciler) or neither; A is recommended and prerequisite for a fully race-free G6.
3. G5: approve moving the aggregate refresh before the rebuild lock with its own short lock.
4. G8: approve the status precedence change and merging exhausted jobs into the next worker run (small; no contract change).
