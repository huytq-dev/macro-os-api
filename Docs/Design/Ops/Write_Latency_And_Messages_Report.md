# Write latency (~5 s), message language, and a real `Updating` repro — report only (2026-10-23)

No code changed for this report. No API started. One read-only prod probe (`SELECT 1` x 30) was run after the user allowed it in this session. Profiling harness: a throw-away project in the session scratchpad (real handlers on a temporary LocalDB database with 120 days of synthetic history, EF command/transaction interceptor counting every round-trip).

## 1. Where the ~5 s goes
### 1.1 Measurements
| What | Value |
|---|---|
| RTT to the prod DB from this machine (`SELECT 1`, 30 runs) | **min 202 / median 203 / p90 205 / max 213 ms** (very stable) |
| First connection open (TLS + login) | **1.54 s** (pooled afterwards) |
| Local compute of a whole write on LocalDB (warm) | 46-201 ms (first call 567 ms = JIT) |

### 1.2 Round-trips per write (real handlers; every SQL statement plus every BEGIN/COMMIT is one network trip)
| Operation | statements + tx ops | round-trips | at 203 ms | + P1 header query (2 statements) |
|---|---|---|---|---|
| Food entry (QuickAdd) today | 19 + 4 | **23** | **4.7 s** | 5.1 s |
| Food entry, 7 / 60 days ago | 18-19 + 4 | 22-23 | 4.5-4.7 s | ≈ 5 s |
| Weight upsert today / 20 days ago | 13 + 2 | **15** | **3.0 s** | 3.4 s |
| Weight upsert 90 days ago | 15 + 4 | 19 | 3.9 s | 4.3 s |
| Derived-status query (the P1 header, `StatusAsync`) | 2 | 2 | 0.4 s | |
Onboarding steps send a weight upsert plus their own reads/writes, so 3-5 s each is the same cause. Local compute is under 2 % of the total: **the time is network round-trips, 203 ms each.**

Breakdown of the food entry (23 trips ≈ 4.7 s):
| Part | Trips | Time |
|---|---|---|
| Handler before the derived part: 3 reads (user locale x2, FoodLog) + FoodItem/entry insert with EF's implicit BEGIN/COMMIT | 6 | 1.2 s |
| `DerivedDataSync` transaction: BEGIN, `sp_getapplock`, 5 aggregate queries, user locale, program+weeks, food logs, weights, expenditure snapshots, daily snapshots, previous TDEE, previous status, one UPDATE, COMMIT | 17 | 3.4 s |
`UserProfiles` is read **3 times** per food write (statements 1, 3, 11) with the same result.

### 1.3 What it is not
- **Not the row budget** (`DerivedSyncRowBudget` 300): a write today touches 1-2 rows. **Not a lock wait**: an uncontended `sp_getapplock` is 1 trip. **Not the algorithm.**
- **Topology:** the E2E API runs on this machine against the remote DB, so every statement pays ~203 ms. A prod API co-located with its DB pays ~1 ms per trip (23 trips ≈ 50-100 ms). I could not measure the deployed API; **compare one write against the MonsterASP API (e.g. via Swagger) to confirm** — if it is fast there, the 5 s is a test-setup artifact, not a product problem.
- **P1 added cost:** my `X-Derived-Data-State` filter adds 2 statements (+0.4 s remote). That part is mine to fix (below).
- **Lock wait vs hold:** on this topology one write holds the user lock ≈ 17 trips = 3.4 s, longer than `DerivedLockWaitMs` (3 s). Two near-simultaneous writes from one user therefore make the second one **Queued** (see §3).

### 1.4 Proposed fixes (no algorithm change; none applied)
| # | Fix | Trips saved | Time saved remote | Risk |
|---|---|---|---|---|
| L1 | Measure/run E2E against an API deployed next to the DB (or a dev API with a local DB) | all but ~0 | 4.5 s → < 0.1 s | none, no code |
| L2 | P1 filter: one-query "open state" instead of the full status (second query only feeds `updatedAt`); later: take the state from the sync outcome (`ReportSync` already knows Completed/Queued/Failed) plus one query only when a job already exists | 1-2 | 0.2-0.4 s | low |
| L3 | Read `UserProfiles` once per request: make `ResolveTimeAsync`, `DerivedDataSync` and `UserLocale.GetAsync` use the scoped `UserClock` cache | 2 | 0.4 s | low |
| L4 | `ExpenditureRebuilder`: previous TDEE and previous status from one query (same row) | 1 | 0.2 s | low |
| L5 | `NutritionAggregates.RefreshAsync`: serve the `NutrientDefinitions` code map from the existing `IReferenceCache` (`uow.Cache`), merge the aggregate reads | 1-3 | 0.2-0.6 s | low |
| L6 | Warm connections: `Min Pool Size=1` (and a short keep-alive) on the connection string to avoid the 1.5 s first-request-after-idle cost | 0 (removes 1.5 s once) | 1.5 s on first request | config only |
| L7 | NOT recommended: return before the rebuild (fire and forget) — would show stale data as fresh, against the freshness requirement; raising `DerivedLockWaitMs` — hides contention instead of fixing it | | | |
L2-L5 together: 23 → about 18 trips (≈ -25 %, 4.7 s → 3.6 s remote). They help but do not replace L1.

### 1.5 Applied 2026-10-23 (user approved L2-L5; results identical, tests added)
| # | Change | Result |
|---|---|---|
| L2 | header filter and check-in gate use `DerivedDataJobs.OpenStateAsync` (1 query, same state rule `StateOf` as `StatusAsync`); new query `GetDerivedDataStateQuery` | header 2 → 1 trip |
| L3 | `UserLocale.GetAsync` remembers (TimeZone, WeekStartDay) per scope for 1 min; "today" still computed from the clock; `UpdateProfile` calls `UserLocale.Forget` | `UserProfiles` 3 reads → 1 |
| L4 | `ExpenditureRebuilder`: previous TDEE and status from one query | -1 trip |
| L5 | already in place: the nutrient code map is cached 10 min via `uow.Cache` (`NutrientSnapshot.cs:23-26`); my first profile used a no-op cache, so it overstated by 1 | no change needed |
Re-measured with the real reference cache (same harness): **food entry 22 → 19 trips (4.5 s → 3.9 s remote), weight upsert 15 → 12 (3.0 s → 2.4 s), header 2 → 1 (0.4 s → 0.2 s)**. With the header: food ≈ 4.1 s (was ≈ 4.9 s), weight ≈ 2.6 s (was ≈ 3.4 s). The remote-DB topology (L1) is still the dominant factor. Tests: `AlgorithmChecks` (StateOf table), `DerivedDataRepro` L2/L3/L4 (statement counts, equal state values, identical snapshots when rebuilding from mid-chain).

## 2. User-facing message sources and a localization approach (product decision)
### 2.1 Where texts come from
| Source | File | Size | Language today |
|---|---|---|---|
| Thrown errors (404/400/409…) | `Contract/Exceptions/ExceptionMessages.cs` | 100 lines | 52 lines Vietnamese, rest English (e.g. `NoActiveProgram` = "Chưa có program đang hoạt động." line 28; `DateInFuture` is English) |
| Validator texts | `Contract/Exceptions/ValidationMessages.cs`, used by 78 validators through `.WithMessage` | 166 lines | 52 lines Vietnamese, rest English |
| Success texts | `Contract/Exceptions/BusinessMessages.cs` | 16 lines | no Vietnamese |
| Inline literals in handlers | 7 `throw new XException("…")` in Application/Infrastructure (4 Vietnamese) | | mixed |
| Generated sentences | e.g. `CheckInCalculator.cs:97` builds a Vietnamese reason sentence; the check-in also returns `ReasonCodes` for the FE to translate | | Vietnamese |
| Generic | `GlobalExceptionHandler` messages (`ExceptionMessages.InternalError`, "Validation failed") | | English/mixed |
Plumbing that exists: `UserProfile.Locale` (`Domain/Entities/Users/UserProfile.cs:16`, saved by `PUT /users/me`). There is **no** request-localization middleware, no `Accept-Language` handling, no resource files.

### 2.2 Options
| | A. Codes only (recommended direction) | B. BE resource files per locale | C. Make everything English |
|---|---|---|---|
| How | every error response already carries `message` (human) and can carry a stable `errors.reasonCode` (pattern used by feasibility, the check-in gate, the derived-data gate); FE maps `reasonCode` → its own i18n string and uses `message` only as a developer fallback | `.resx` (or a dictionary) per locale in Contract; pick by `Accept-Language` or `UserProfile.Locale`; translate the ~100 + 166 + 16 strings | translate the 52 + 52 Vietnamese lines |
| Pros | FE owns all UI text (one place, already has i18n for check-in reason codes); BE stays language-neutral | works for third-party clients | smallest, fixes the "Vietnamese in English UI" immediately |
| Cons | needs a code for each message: ~150 throw sites need `reasonCode` added (mechanical, additive contract) | two translation catalogs to keep in sync; the validator messages carry parameters | still one language; Vietnamese users get English |
| Contract change | additive (`errors.reasonCode`) | none | none |
Suggested path (needs your decision): **C now** (small, removes the visible inconsistency), then **A progressively** starting with messages the UI actually shows (program/goal/check-in/food-log/weights), keeping `message` as the English fallback. B only if a non-FE client appears.
The 404 in the report (`DELETE /programs/current` → `NoActiveProgram`) would become `errors.reasonCode = "NO_ACTIVE_PROGRAM"` + English message.

## 3. A real `X-Derived-Data-State: Updating` with the TEST account (described, NOT run)
All three write only to the test account's own data.
1. **Two simultaneous writes (recommended, no config, no restart).** On this topology a write holds the user lock ≈ 3.4 s (§1.3) and the second request waits only `DerivedLockWaitMs` = 3 s (`DerivedDataSync.cs:46`). From the browser, send two writes at once (e.g. `Promise.all([POST /weights {date: today}, POST /food-logs/quick-add])`, or two quick-adds). Expected: the first response `Ready`, the second `Updating` (its rebuild was queued as a durable job; `uow.Derived.Wake()` makes the worker pick it up at once, so status returns to `Ready` after ≈ 3-5 s). Poll `GET /api/derived-data/status` meanwhile: `Updating`, `fromDate/toDate` of the job, then `Ready`.
2. **Deterministic, needs an API restart:** start the API with the environment variable `Algorithm__DerivedSyncRowBudget=0`. Then `DerivedDataSync.RunAsync` takes the `bounded && budget <= 0` branch (`DerivedDataSync.cs:35-41`) for every write: no rebuild in the request, a Pending job is created, the header says `Updating`, the worker (left enabled) completes it within one poll (≤ 10 s, immediately when woken). Unset the variable afterwards. Do **not** also set `Algorithm__BackgroundDerivedProcessingEnabled=false`: the job would stay Pending forever on prod data.
3. **Not reproducible safely:** a big backdated edit does not exceed the 300-row budget for a small test account (a few days of data), so it completes inside the request and stays `Ready`. `Failed` needs 5 failed attempts of a durable job (a rebuild exception); there is no safe trigger, keep simulating it in the browser.
