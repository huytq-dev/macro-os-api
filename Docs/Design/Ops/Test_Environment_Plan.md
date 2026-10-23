# Isolated test environment — PLAN ONLY (nothing applied)

User spec (verbatim): "MOS-BE chạy với Test/Development configuration. SQL Server test database riêng. Connection string tách hoàn toàn khỏi production. AutoMigration mặc định OFF. Chỉ áp dụng migrations trên test DB sau khi tôi duyệt kế hoạch. Tạo tài khoản test và xác thực email trong test DB, hoặc dùng test-only email verification flow. MOS-FE kết nối API test."

## 1. Why it is needed (facts)
- One config file, `src/API/appsettings.json`, holds the prod DB (`db71151` on `databaseasp.net`), SMTP (`EmailSettings`), Cloudinary, JWT, Frontend URL (`macro-os-five.vercel.app`). `launchSettings.json` profiles only set `ASPNETCORE_ENVIRONMENT=Development`; there is no `appsettings.Development.json`. A plain `dotnet run` therefore uses prod (`Prod_Safety_Audit_2026-10-23.md` §4).
- `Program.cs:19` always runs `MigrateAsync` + 4 seeds (`ApplicationBuilderExtensions.cs:14-21`); no switch.
- Prod deploy (`.github/workflows/deploy-monsterasp.yml`) only publishes and WebDeploys files; **the migrations of prod are applied by the app itself at start** (`ApplyMigrationsAsync`). Turning that off by default without a replacement would silently stop schema updates on prod.

## 2. Proposed design
| Piece | Proposal | Code touched |
|---|---|---|
| Environment name | `ASPNETCORE_ENVIRONMENT=Test` | none |
| Config | new `src/API/appsettings.Test.json` (test-only values, **not** the prod file): `ConnectionStrings:DefaultConnection` → LocalDB `MacroOS_IT`, `Database:AutoMigrate=false`, `Algorithm:BackgroundDerivedProcessingEnabled=false`, `Email:Provider=Log`, `Frontend:BaseUrl`/`AllowedOrigins` = the test FE URL, Cloudinary/Ai/Usda keys empty, own JWT key (tokens from one env never valid in the other). Env vars (`ConnectionStrings__DefaultConnection`, …) can override at launch | `.json` only |
| Auto-migrate switch | `Database:AutoMigrate` (bool), code default **false** (as the user asked). `ApplyMigrationsAsync` calls `MigrateAsync` + seeds only when true; when false it logs `Pending migrations: N` and starts **without** touching schema or seed (reads `GetPendingMigrationsAsync`, no write) | `ApplicationBuilderExtensions.cs` (≈ 6 lines) |
| Prod keeps migrating | see §3 | `appsettings.json` or `appsettings.Production.json` |
| Cross-environment guard | at startup, if environment ≠ Production and the connection string host is not `localhost`/`(localdb)`/`.`/`127.0.0.1`, refuse to start unless `Database:AllowRemote=true`. Prevents "dev/test pointing at prod" by accident | ≈ 8 lines in the same extension |
| Background work in Test | `Algorithm:BackgroundDerivedProcessingEnabled=false` already disables both the nightly Quartz job (`QuartzExtensions.cs:8`) and the worker loop (`DerivedDataWorker.cs:23`). Synchronous rebuilds on write stay (they are the behaviour under test) | none |
| Email sink | add `LogEmailSender : IEmailSender` selected by `Email:Provider=Log`: writes recipient, subject and the verification link/token to the log (and optionally to `.it/outbox/*.json`); no SMTP connection. Default (`Smtp`) unchanged | 1 class + 1 branch in DI |
| Test account + verification | (a) **no code**: register through the API on the test DB, then `UPDATE Users SET EmailVerified=1 WHERE Email=@email` on LocalDB (allowed by the user: "trong test DB"); or (b) take the `confirm-email?token=…` link from the log sink and call the confirm-email endpoint — exercises the real flow, still no SMTP. Recommend (b) for the flow, (a) as fallback. No test-only HTTP endpoint (it would ship to prod) | none for (a)/(b) |
| Token store | `Upstash:BaseUrl` is empty in appsettings → `MemoryCacheService` already used; keep empty in Test so verification tokens never touch a shared Redis | none |
| Test DB | LocalDB `(localdb)\MSSQLLocalDB`, DB `MacroOS_IT` (currently absent). Schema: **after approval**, one `dotnet ef database update` against that connection (all 30 migrations) and a one-off `AutoMigrate=true` start for seeds. Re-creatable: drop + update | none |
| FE | MOS-FE points its API base URL (own env file) at the Test API (`https://localhost:7000`); `AllowedOrigins` in `appsettings.Test.json` lists the FE dev origin | FE env + json |
| Launch | `.it/api.ps1` (existing, gitignored, LocalDB-only) switches to `ASPNETCORE_ENVIRONMENT=Test` and refuses a non-LocalDB connection string | local script |
| Seed volume | with `AutoMigrate=true` the first start seeds the catalog (same seeds as prod) — slow once, idempotent after | none |

## 3. How prod keeps applying approved migrations if the default is OFF (user's call)
| Option | How | + | − |
|---|---|---|---|
| **A (recommended)** | `appsettings.Production.json` (published by the same WebDeploy; MonsterASP runs `Production` by default) with `Database:AutoMigrate=true`; base `appsettings.json` has `false`. Deploy behaviour **identical to today** | zero workflow change; Development/Test/unknown env never migrate | the "on" switch lives in a repo file; move the prod connection string there too (next paragraph) so Development/Test inherit **no** prod values |
| A2 | set `Database__AutoMigrate=true` as an environment variable in the MonsterASP site settings | flag not in repo | invisible in review; lost if the site is recreated |
| B | explicit migration step in the workflow (`dotnet ef migrations bundle` → run against prod with a CI secret) before WebDeploy | migrations become an auditable, approved deploy step; app never needs DDL rights | needs a GitHub secret with prod DB credentials and a network path from the runner (MonsterASP SQL is public, so possible); more moving parts; app must tolerate "schema one step behind" during the swap |
| C | keep `true` everywhere except Development/Test (guard by environment name) | smallest | the user asked default OFF; one mis-set `ASPNETCORE_ENVIRONMENT` still migrates prod |

Related: move the prod `DefaultConnection`, SMTP, Cloudinary and JWT values out of `appsettings.json` into `appsettings.Production.json` (or env vars). Then Development/Test start with **no** prod values and fail fast (`JwtOptions.EnsureValid`, empty connection string) instead of silently using prod. Repo must stay private either way (CLAUDE.md).

## 4. Order of work after approval
1. `Database:AutoMigrate` flag + guard + `LogEmailSender` (+ pure checks: flag default false, guard host rule, log sink).
2. `appsettings.Test.json`, `.it` launcher update.
3. Prod config per the option chosen in §3 (**before** the flag lands in a deploy, otherwise the next prod deploy would skip migrations).
4. User approval → create `MacroOS_IT`, `dotnet ef database update`, seed, create the test account, hand the URL to MOS-FE.
5. Remove the prod test account (`e2e-macros-7k3q@example.test`) only when the user explicitly says so.

## 5. Not done
No code, config, migration or DB change was made for this plan.
