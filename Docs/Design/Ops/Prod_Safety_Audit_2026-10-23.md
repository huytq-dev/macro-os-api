# Prod safety audit — 2026-10-23 (MOS-BE, read-only)

Trigger: user revoked production testing ("Dừng API local đang kết nối production …"). MOS-Master's API ran on `https://localhost:7000` against the **production** DB (`db71151`, because `src/API/appsettings.json` is the only config file and `ConnectionStrings:DefaultConnection` in it is the prod DB) from about 14:03 (+07) = 07:03 UTC until Master stopped it (~17:1x +07). Master reports one restart.

Method: code reading for §1; read-only SELECTs (counts / timestamps / migration ids only, no personal data) for §2–§3, run after the user confirmed **in this session** ("Cho phép SELECT"). One extra SELECT (per-user flags, to identify who modified the existing user row) was blocked by the classifier and **not** worked around. No write, no rollback by me during this audit.

## 0. The one write I made on prod in this window
| | |
|---|---|
| time | 2026-10-23 ≈ 09:13:19 UTC (16:13 +07), right after the user's direct "Confirm" in my session |
| statement | `UPDATE [Users] SET [EmailVerified] = 1 WHERE [Email] = @email AND ([EmailVerified] IS NULL OR [EmailVerified] = 0)` with `@email = 'e2e-macros-7k3q@example.test'`, inside a transaction that commits only if `ROWS AFFECTED = 1` |
| result | 1 row, committed, read-back `EmailVerified = 1` |
| note | raw SQL: does **not** change `Users.ModifiedAt` (the interceptor is not involved). Not reverted (user instruction). |

## 1. Startup side effects (from code)
Entry: `src/API/Program.cs`.
| # | Where | What it does at start | Writes on an already-seeded DB? |
|---|---|---|---|
| 1 | `Program.cs:13` `TryRunDevCommandAsync(args)` (`DevCommandExtensions.cs:22`) | maintenance commands only when args are passed **and** `ASPNETCORE_ENVIRONMENT=Development`; plain `dotnet run` does nothing here | no |
| 2 | `Program.cs:19` → `ApplicationBuilderExtensions.cs:14` `db.Database.MigrateAsync()` | applies every migration in the built assembly that is missing in `__EFMigrationsHistory`. Runs on **every** start in **every** environment; there is no flag | only if the build is newer than the DB. Latest migration in code = `20261023024227_S27_BodyMetricUniqueDay` (no S28 exists) and prod history already ends at S27 → nothing pending |
| 3 | `ApplicationBuilderExtensions.cs:18` `FoodData.SeedAsync` (`FoodData.cs:127-168`) | inserts catalog foods whose `ExternalFoodId` is missing; `SeedAliasesAsync` (`:108-125`) inserts missing en-US aliases | idempotent: no insert when nothing is missing (`:136`, `if (added)`) |
| 4 | `:19` `NutrientData.SeedAsync` (`NutrientData.cs:224-267`) | inserts missing `NutrientDefinition` codes; fills `UsdaNumber` only when null | idempotent; `SaveChanges` only `if (changed)` |
| 5 | `:20` `CanonicalCatalogData.SeedAsync` (`CanonicalCatalogSeeder.cs:5-110`, `SeedCondimentsAsync :130`, `EnsureCountableServingsAsync :97`) | inserts missing Vietnamese foods / canonicals / source records / aliases / condiments / countable servings; sets `CanonicalFoodId` only when null (`:48`, `:172`) | idempotent by key; writes only for rows missing from the DB (e.g. if the local build's seed lists were newer than prod's deployed build) |
| 6 | `:21` `FoodIconBackfill.RunAsync` (`FoodIconBackfill.cs:11-28`) | sets `IconKey` on non-user foods that have it null | idempotent; rows the resolver cannot map stay null and are re-read each start (no write) |
| 7 | Quartz hosted service `QuartzExtensions.cs:26` + `ExpenditureNightlyJob` (`:16-22`, cron `0 30 * * * ?` UTC = every hour at :30) | registered **unless** `Algorithm:BackgroundDerivedProcessingEnabled=false` (`:8`). `appsettings.json` has no such key → default `true` (`AlgorithmOptions.cs:42`). Per hour it rebuilds "yesterday + today" snapshots for active-program users whose **local** hour is 00 (`ExpenditureNightlyJob.cs:22-42`) | **can write** (`ExpenditureSnapshots`, derived rows) only for users due at that hour |
| 8 | `DerivedDataWorker` (`DerivedDataWorker.cs:20-39`, registered `ServiceCollectionExtensions.cs:27`) | loop: `RunPendingAsync` every `DerivedWorkerPollSeconds`: claims Pending / stale-Running / retryable Failed `RecalculationJobs` (`:51-60`, claim `:81-88`) and rebuilds; **also** `ExecuteDeleteAsync` of Succeeded jobs older than 7 days (`:62`) at every pass | can write and **delete** (the delete leaves no timestamp trace). Same disabled flag `:23` |
| 9 | Email: `QuartzBackgroundJobService.EnqueueEmailAsync` → `EmailSender.cs:20-51` (real SMTP from `EmailSettings`) | on demand only (register, resend, forgot-password) | not at startup; the test-account registration would have sent one verification email to `…@example.test` |

Unknown (ask Master): whether his launch set `Algorithm__BackgroundDerivedProcessingEnabled=false`. If not, rows 7 and 8 were live against prod for ≈ 3 h.

**Double execution with the MonsterASP prod API** (same code, same DB):
- `RecalculationJob`: the claim is a conditional `UPDATE … WHERE State/lease` (`DerivedDataWorker.cs:81-88`); a loser gets 0 rows → skipped. Per-user `sp_getapplock` makes the second instance return `Busy` → job released to Pending without consuming an attempt (`:93`). Safe by design.
- `ExpenditureNightlyJob`: `[DisallowConcurrentExecution]` + `UseInMemoryStore` (`QuartzExtensions.cs:14`) only protects **inside one process**; two processes each fire their own trigger. Protection is the user lock + idempotent overwrite ("dựng lại là phép ghi đè cùng kết quả"). No duplicate rows expected, but double work, and a version-stamp race if the two builds carry different `AlgorithmVersion` (the rebuilder refuses to overwrite newer versions, `ExpenditureRebuilder.cs:77-83`).
- Migrations: two instances starting together both run `MigrateAsync`; EF takes a DB-level migration lock, second waits then finds nothing pending.
- Seeds: two instances starting together could both pass the "missing" check and insert the same seed rows; unique indexes on `(ProviderCode, ExternalFoodId)` would turn the loser into a startup failure rather than duplicates (not re-verified here).

## 2. Migration history on prod (SELECT, 2026-10-23 10:17 UTC)
`SELECT COUNT(*) FROM __EFMigrationsHistory` → **30**. Top 8 by id:
```
20261023024227_S27_BodyMetricUniqueDay
20261022152259_S26_ProgramWeekDayTargets
20261022080214_S25_ExpenditureSettings
20261021082014_S24_NutrientGoalHistory
20261021015953_S23_FoodLogFasting
20261020145147_S22_RecipeCreatedByFk
20261020144107_S21_RecipePreparation
20261020141819_S20_FoodLogNoteNameColor
```
Confirmed: nothing beyond S27 (and none exists in code). S25 + S26 were applied 2026-10-22 on the user's approval "Áp S25 + S26 ngay" (Master); S27 today on "Production + áp S27 + tài khoản test" (me). Neither was applied by accident in this window.

## 3. Writes during the window (rows with CreatedAt or ModifiedAt ≥ 2026-10-23 07:00:00 UTC)
Generic query over **every** non-`bak_*` table having both columns; only these tables returned rows:
| table | created in window | modified in window (created earlier) | latest |
|---|---|---|---|
| `Users` | 1 | 1 | created 08:43:30 UTC (test registration); modified 08:19:31 UTC |
| `UserProfiles` | 1 | 0 | created 08:43:30 UTC |

Everything else shows **no** row: `RecalculationJobs` (table is empty: 0 rows total, 0 started in window), `ExpenditureSnapshots` (7 rows; newest `CreatedAt` 2026-10-23 00:30:00 UTC = the prod server's own nightly job at 07:30 +07, before the window; newest `ModifiedAt` 2026-10-21), `FoodItems`, `FoodAliases`, `CanonicalFoods`, `FoodServings`, `FoodSourceRecords`, `NutrientDefinitions`, all food-log/goal/program/weight tables. `Users` total = 2.

Reading:
- The seeds and the icon backfill wrote nothing (no catalog row created/modified).
- No snapshot / job was written by the nightly job or the worker. Consistent with either (a) flag was off, or (b) no user was due: nightly runs `:30` UTC and only for users whose local hour is 00.
- The test registration (Users + UserProfiles, 08:43 UTC) is the expected write.
- One **existing** user row was modified at 08:19:31 UTC, **before** the test registration and before my UPDATE (09:13). Most likely a login/refresh-token write (`Users.RefreshToken`, `RefreshTokenExpiry` live on the row). **Not confirmed**: my identifying SELECT was blocked and I did not retry.
- Not visible by timestamp: hard deletes (`DerivedDataWorker.cs:62` deletes Succeeded jobs > 7 days; `RecalculationJobs` is empty now, so I cannot tell whether it held rows before) and raw-SQL writes (mine, §0). Tables with no `CreatedAt`: only `__EFMigrationsHistory`.

## 4. Root exposure and what to change (proposal, not applied)
`src/API/appsettings.json` is the only config and carries the prod connection string, SMTP, Cloudinary and JWT values (CLAUDE.md "Chỉ có MỘT file cấu hình"). So every plain `dotnet run` (Development) = prod DB + `MigrateAsync` + seeds + background jobs + real SMTP. See `Test_Environment_Plan.md`.

## C. Food Log PUT 500 (task C)
- Not reproduced from a real trace: `BE/.it/api.log` (the earlier LocalDB run) contains **no** PUT/DbUpdateConcurrency entry; its only `ERR` is an unrelated `SecurityTokenInvalidSignatureException` from `POST /auth/refresh-token` with a malformed token (`JwtTokenService.cs:86`), which `GlobalExceptionHandler` turns into **500 instead of 401** — separate small bug, not fixed (not requested).
- Root cause (code + EF-level repro): two overlapping `PUT /food-logs/entries/{id}` both read the RowVersion of `FoodLogEntry` (`FoodLogEntryConfiguration.cs:14`) and `FoodLog` (`FoodLogConfiguration.cs:27`); the later `SaveChanges` throws `DbUpdateConcurrencyException`, which no handler/exception mapper handled → 500. If MOS-FE's two PUTs are strictly sequential (second sent after the first response), this cause does **not** apply and a trace (traceId from the 500 body → "Unhandled exception" log line) is needed.
- Fix (root cause, not a catch-all): `UpdateFoodLogEntryCommandHandler.cs:8-9` runs the write through `IdempotentWrite.RunAsync` (retry once on a fresh read, then 409); `GlobalExceptionHandler.cs:14,29` maps any other `DbUpdateConcurrencyException` to 409 rather than 500.
- Fail-before / pass-after: `tests/ConcurrencyRepro` (own temp LocalDB DB, no shared DB): without the wrapper the second PUT throws `DbUpdateConcurrencyException`; with it attempts = 2, stored quantity = last writer, persistent conflict = `ConflictException` 409. Pure check in `tests/AlgorithmChecks` (843 PASS).
- Partial updates cannot persist: the handler changes entry + macros + `NutrientSnapshot.ScaleAsync` + `FoodLog` totals tracked in one `DbContext`; they are saved by the single `SaveChanges` in `EnsureSaveAsync` (`FoodLogSupport.cs:177`) = one SQL transaction (all or none). On conflict the retry does `DiscardChanges()` (clears the tracker) and re-reads before re-applying; the nutrient scaling is by ratio against the persisted values, and a retry after a persisted first save sees `plan.Unchanged` → no double scaling. The derived rebuild after the save (`DerivedDataSync`, `FoodLogSupport.cs:180`) is intentionally separate: if it fails the edit stays and a durable job finishes it.
