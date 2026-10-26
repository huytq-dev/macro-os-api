// Phiên bản thuật toán lệch nhau (build cũ dựng lại sau build mới): snapshot expenditure do phiên bản MỚI HƠN dựng không bị ghi đè (ExpenditureRebuilder), NHƯNG số liệu NGUỒN của ngày
// (DailyProgressSnapshot: kcal/macro đã ăn) không phụ thuộc phiên bản thuật toán nên vẫn phải được làm mới theo log vừa sửa. Chạy trên LocalDB tạm, tạo mới rồi xoá.
using System.Security.Claims;
using API;
using Application;
using Contract;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_DerivedRepro";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";   // MARS như prod (EF tắt savepoint)
var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;
ApplicationDbContext NewCtx() => new(opts);

async Task Drop()
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand();
    cmd.CommandText = $"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END";
    await cmd.ExecuteNonQueryAsync();
}

var failed = 0;
void Check(string name, bool ok, string detail) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name} — {detail}"); if (!ok) failed++; }

var userId = Guid.NewGuid();
var d0 = new DateOnly(2026, 10, 1);
var last = d0.AddDays(4);
var edited = d0.AddDays(2);
var newer = new AlgorithmOptions { FreezeTdeeDuringCalibration = false };   // nhãn "7" (học từ ngày 1)
var older = new AlgorithmOptions();                                          // nhãn "6" (mặc định hiện tại)

await Drop();
try
{
    using (var setup = NewCtx())
    {
        await setup.Database.MigrateAsync();
        await setup.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");   // dữ liệu thử, không cần user/food thật
        setup.Add(new MacroProgram { Id = Guid.NewGuid(), UserId = userId, StartDate = d0, ExpenditureStartDate = d0, ManualInitialTdeeKcal = 2500, IsActive = true });
        for (var i = 0; i <= 4; i++)
        {
            var logId = Guid.NewGuid();
            setup.Add(new FoodLog { Id = logId, UserId = userId, LogDate = d0.AddDays(i), TotalCalories = 2000 });
            setup.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = 2000 });
        }
        await setup.SaveChangesAsync();
    }

    // 1) Build MỚI dựng lại: mọi snapshot mang nhãn "7", daily progress = 2000 kcal
    using (var c = NewCtx()) { await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), userId, d0, last, newer, default, null, last); await c.SaveChangesAsync(); }

    decimal tdeeBefore;
    using (var c = NewCtx())
    {
        var ex = await c.Set<ExpenditureSnapshot>().Where(s => s.UserId == userId).ToListAsync();
        Check("setup: snapshot do build mới mang nhãn 7", ex.Count == 5 && ex.All(s => s.AlgorithmVersion == "7"), $"{ex.Count} snapshot, nhãn {string.Join(",", ex.Select(s => s.AlgorithmVersion).Distinct())}");
        tdeeBefore = ex.First(s => s.SnapshotDate == edited).EstimatedTdeeKcal;
    }

    // 2) User sửa log ngày 'edited' (2000 → 2600 kcal)
    using (var c = NewCtx())
    {
        var log = await c.Set<FoodLog>().FirstAsync(l => l.UserId == userId && l.LogDate == edited);
        log.TotalCalories = 2600;
        var entry = await c.Set<FoodLogEntry>().FirstAsync(e => e.FoodLogId == log.Id);
        entry.Calories = 2600;
        await c.SaveChangesAsync();
    }

    // 3) Build CŨ (nhãn "6") dựng lại từ ngày sửa
    using (var c = NewCtx()) { await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), userId, edited, last, older, default, null, last); await c.SaveChangesAsync(); }

    using (var c = NewCtx())
    {
        var ex = await c.Set<ExpenditureSnapshot>().FirstAsync(s => s.UserId == userId && s.SnapshotDate == edited);
        Check("bảo vệ phiên bản: build cũ KHÔNG ghi đè snapshot expenditure nhãn 7", ex.AlgorithmVersion == "7" && ex.EstimatedTdeeKcal == tdeeBefore, $"nhãn {ex.AlgorithmVersion}, TDEE {ex.EstimatedTdeeKcal} (trước {tdeeBefore})");

        var daily = await c.Set<DailyProgressSnapshot>().FirstAsync(s => s.UserId == userId && s.SnapshotDate == edited);
        Check("daily progress theo log vừa sửa dù build cũ dựng lại", daily.CaloriesConsumed == 2600, $"CaloriesConsumed = {daily.CaloriesConsumed} (log mới 2600; 2000 = số cũ, chưa được làm mới)");
    }

    // 4) P5: Reset / SetManual dưới khoá user. Một lần dựng/worker đang giữ khoá ⇒ ghi chờ rồi 409 (không chen vào); nhả khoá ⇒ ghi thành công
    var shortWait = Microsoft.Extensions.Options.Options.Create(new AlgorithmOptions { DerivedLockWaitMs = 300 });
    var me = new TestUser(userId);
    async Task<bool> ConflictsWhileLocked(Func<IUnitOfWork, Task> run)
    {
        using var holderCtx = NewCtx();
        var holder = new UnitOfWork(holderCtx);
        await holder.BeginTransactionAsync();
        if (!await holder.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(userId), 1000)) throw new InvalidOperationException("holder could not take the lock");
        try
        {
            using var c = NewCtx();
            await run(new UnitOfWork(c));
            return false;                                   // không bị chặn = ghi chen vào giữa lần dựng
        }
        catch (ConflictException) { return true; }
        finally { await holder.RollbackTransactionAsync(); }
    }

    var resetBlocked = await ConflictsWhileLocked(u => new ResetExpenditureCommandHandler(u, me, TimeProvider.System, shortWait).Handle(new ResetExpenditureCommand(), default));
    Check("P5 Reset: lần dựng đang giữ khoá user ⇒ 409 (không ghi chen vào)", resetBlocked, resetBlocked ? "ConflictException" : "đã ghi mà không chờ khoá");

    using (var c = NewCtx()) { var p = await c.Set<MacroProgram>().FirstAsync(x => x.UserId == userId); p.ProgramType = ProgramType.Manual; await c.SaveChangesAsync(); }   // bỏ nhánh cần cân nặng
    var manualBlocked = await ConflictsWhileLocked(u => new SetManualExpenditureCommandHandler(u, me, shortWait, TimeProvider.System).Handle(new SetManualExpenditureCommand(2700), default));
    Check("P5 SetManual: lần dựng đang giữ khoá user ⇒ 409 (không ghi chen vào)", manualBlocked, manualBlocked ? "ConflictException" : "đã ghi mà không chờ khoá");

    using (var c = NewCtx()) await new ResetExpenditureCommandHandler(new UnitOfWork(c), me, TimeProvider.System, shortWait).Handle(new ResetExpenditureCommand(), default);
    using (var c = NewCtx()) await new SetManualExpenditureCommandHandler(new UnitOfWork(c), me, shortWait, TimeProvider.System).Handle(new SetManualExpenditureCommand(2700), default);
    using (var c = NewCtx())
    {
        var p = await c.Set<MacroProgram>().FirstAsync(x => x.UserId == userId);
        var snap = await c.Set<ExpenditureSnapshot>().Where(s => s.UserId == userId).OrderByDescending(s => s.SnapshotDate).FirstAsync();
        Check("P5: không có khoá tranh chấp ⇒ Reset + SetManual vẫn ghi đúng", p.ManualInitialTdeeKcal == 2700 && snap.EstimatedTdeeKcal == 2700 && snap.Status == ExpenditureStatus.Calibrating && p.ExpenditureStartDate == snap.SnapshotDate,
            $"manual {p.ManualInitialTdeeKcal}, snapshot {snap.SnapshotDate} {snap.EstimatedTdeeKcal} {snap.Status}, start {p.ExpenditureStartDate}");
    }

    // 5) P1: header X-Derived-Data-State do filter thật gắn từ trạng thái thật trong DB
    async Task<string?> HeaderFor(string method, string path, IActionResult result, bool authenticated = true)
    {
        using var c = NewCtx();
        var sender = new StatusSender(new UnitOfWork(c), me, shortWait.Value);
        var http = new DefaultHttpContext();
        http.Request.Method = method; http.Request.Path = path;
        if (authenticated) http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        var filter = new DerivedDataStateFilter(sender, Microsoft.Extensions.Logging.Abstractions.NullLogger<DerivedDataStateFilter>.Instance);
        await filter.OnActionExecutionAsync(executing, () => Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()) { Result = result }));
        return http.Response.Headers.TryGetValue(DerivedDataStateHeader.Name, out var v) ? v.ToString() : null;
    }
    var ok = new OkObjectResult(1);
    var ready = await HeaderFor("PUT", "/api/food-logs/entries/x", ok);
    Check("P1 Ready: không còn job ⇒ header Ready", ready == "Ready", $"header = {ready ?? "(none)"}");

    using (var c = NewCtx()) { c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = userId, Cause = RecalculationCause.FoodLogChanged, FromDate = d0, ToDate = last, State = JobState.Pending }); await c.SaveChangesAsync(); }
    var updating = await HeaderFor("POST", "/api/weights", ok);
    Check("P1 Updating: có job Pending ⇒ header Updating", updating == "Updating", $"header = {updating ?? "(none)"}");

    using (var c = NewCtx()) { var j = await c.Set<RecalculationJob>().FirstAsync(x => x.UserId == userId); j.State = JobState.Failed; j.AttemptCount = 5; await c.SaveChangesAsync(); }
    var failedHeader = await HeaderFor("DELETE", "/api/weights/2026-10-01", new NoContentResult());
    Check("P1 Failed: job hết lượt thử ⇒ header Failed (kể cả 204)", failedHeader == "Failed", $"header = {failedHeader ?? "(none)"}");

    var onGet = await HeaderFor("GET", "/api/expenditure", ok);
    var onError = await HeaderFor("PUT", "/api/food-logs/entries/x", new ObjectResult("conflict") { StatusCode = 409 });
    var onAuth = await HeaderFor("POST", "/api/auth/logout", ok);
    var anonymous = await HeaderFor("POST", "/api/weights", ok, authenticated: false);
    Check("P1: không gắn header cho GET, lỗi 409, /api/auth, người chưa đăng nhập", onGet is null && onError is null && onAuth is null && anonymous is null, $"GET={onGet ?? "-"} 409={onError ?? "-"} auth={onAuth ?? "-"} anon={anonymous ?? "-"}");

    // 6) G6: Accept check-in bị chặn khi dẫn xuất chưa Ready; Reject luôn được; preview mang trạng thái. Handler/Support thật trên DB tạm, mỗi kịch bản một user riêng
    var ciToday = DateOnly.FromDateTime(DateTime.UtcNow);
    var ciOpt = new AlgorithmOptions { DerivedLockWaitMs = 300 };
    async Task<Guid> SeedCheckInUser(JobState? job)
    {
        var u = Guid.NewGuid();
        using var c = NewCtx();
        var start = ciToday.AddDays(-14);
        var program = new MacroProgram
        {
            Id = Guid.NewGuid(), UserId = u, ProgramType = ProgramType.Coached, GoalType = GoalType.WeightLoss, StartDate = start, ExpenditureStartDate = start, ManualInitialTdeeKcal = 2500,
            CheckInWeekday = ciToday.DayOfWeek, WeeklyWeightChangeTargetPercent = 0.5m, WeeklyWeightChangeTargetKg = -0.4m, ProteinGPerKgBodyweight = 2, IsActive = true,
        };
        program.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = start, CaloriesTarget = 2000, ProteinTarget = 150, CarbsTarget = 200, FatTarget = 60, AdjustmentReason = AdjustmentReason.Initial });
        c.Add(program);
        c.Add(new WeightEntry { Id = Guid.NewGuid(), UserId = u, LogDate = ciToday.AddDays(-8), WeightKg = 80m, TrendWeightKg = 80m });
        c.Add(new WeightEntry { Id = Guid.NewGuid(), UserId = u, LogDate = ciToday, WeightKg = 79.6m, TrendWeightKg = 79.6m });
        c.Add(new ExpenditureSnapshot { Id = Guid.NewGuid(), UserId = u, SnapshotDate = ciToday, EstimatedTdeeKcal = 2500, Status = ExpenditureStatus.Updating, AlgorithmVersion = "6", AvgCaloriesConsumed = 2200, WindowDays = 14 });
        if (job is { } state) c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = u, Cause = RecalculationCause.FoodLogChanged, FromDate = ciToday.AddDays(-1), ToDate = ciToday, State = state, AttemptCount = state == JobState.Failed ? 5 : 0 });
        await c.SaveChangesAsync();
        return u;
    }
    async Task<(int CheckIns, int Weeks)> Counts(Guid u)
    {
        using var c = NewCtx();
        return (await c.Set<WeeklyCheckIn>().CountAsync(x => x.UserId == u), await c.Set<MacroProgramWeek>().CountAsync(w => w.MacroProgram.UserId == u));
    }
    async Task<(string? Reason, Guid? Id)> Decide(Guid u, bool accept)
    {
        using var c = NewCtx();
        try { return (null, await CheckInSupport.DecideAsync(new UnitOfWork(c), u, ciToday, ciOpt, accept, default)); }
        catch (ConflictException e) { return (e.Errors is { } er && er.TryGetValue("reasonCode", out var v) ? v[0] : "CONFLICT:" + e.Message, null); }
    }

    var uUpd = await SeedCheckInUser(JobState.Pending);
    var accUpd = await Decide(uUpd, true);
    var cntUpd = await Counts(uUpd);
    Check("G6 Updating: Accept ⇒ 409 DERIVED_DATA_UPDATING, không ghi tuần/check-in", accUpd.Reason == "DERIVED_DATA_UPDATING" && cntUpd == (0, 1), $"reason {accUpd.Reason}, check-ins {cntUpd.CheckIns}, weeks {cntUpd.Weeks}");

    var uFail = await SeedCheckInUser(JobState.Failed);
    var accFail = await Decide(uFail, true);
    var cntFail = await Counts(uFail);
    Check("G6 Failed: Accept ⇒ 409 DERIVED_DATA_FAILED, không ghi gì", accFail.Reason == "DERIVED_DATA_FAILED" && cntFail == (0, 1), $"reason {accFail.Reason}, check-ins {cntFail.CheckIns}, weeks {cntFail.Weeks}");

    var rejFail = await Decide(uFail, false);
    var cntRej = await Counts(uFail);
    bool? storedAccepted;
    using (var c = NewCtx()) storedAccepted = await c.Set<WeeklyCheckIn>().Where(x => x.UserId == uFail).Select(x => (bool?)x.Accepted).SingleOrDefaultAsync();
    Check("G6: Reject vẫn được khi Failed (chỉ chép target cũ) và LƯU đúng Accepted = false", rejFail.Reason is null && rejFail.Id is not null && cntRej == (1, 2) && storedAccepted == false, $"reason {rejFail.Reason ?? "-"}, check-ins {cntRej.CheckIns}, weeks {cntRej.Weeks}, stored Accepted {storedAccepted?.ToString() ?? "null"}");

    var uReady = await SeedCheckInUser(null);
    var accReady = await Decide(uReady, true);
    using (var c = NewCtx())
    {
        var ci = await c.Set<WeeklyCheckIn>().Where(x => x.UserId == uReady).ToListAsync();
        Check("G6 Ready: Accept thành công, lưu TDEE của snapshot", accReady.Reason is null && ci.Count == 1 && ci[0].Accepted && ci[0].TdeeEstimate == 2500, $"reason {accReady.Reason ?? "-"}, check-ins {ci.Count}, tdee {ci.FirstOrDefault()?.TdeeEstimate}");
    }

    // Đã Accept xong: bấm đôi khi có job mới vẫn trả Id cũ (nhánh idempotent đứng trước cổng)
    using (var c = NewCtx()) { c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uReady, Cause = RecalculationCause.FoodLogChanged, FromDate = ciToday, ToDate = ciToday, State = JobState.Pending }); await c.SaveChangesAsync(); }
    var again = await Decide(uReady, true);
    Check("G6: Accept lặp sau khi đã Accept ⇒ cùng Id, không 409 dù có job mới", again.Reason is null && again.Id == accReady.Id, $"reason {again.Reason ?? "-"}, same id {again.Id == accReady.Id}");

    // Preview mang trạng thái
    async Task<(string State, string? Reason)> PreviewOf(Guid u)
    {
        using var c = NewCtx();
        var ctx = await CheckInSupport.BuildAsync(new UnitOfWork(c), u, ciToday, ciOpt, default);
        return (ctx.Preview.DerivedDataState, ctx.Preview.AcceptBlockedReason);
    }
    var pUpd = await PreviewOf(uUpd); var pFail = await PreviewOf(uFail);
    var uReady2 = await SeedCheckInUser(null); var pReady = await PreviewOf(uReady2);
    Check("G6 preview: derivedDataState + acceptBlockedReason theo trạng thái", pUpd == ("Updating", "DERIVED_DATA_UPDATING") && pFail == ("Failed", "DERIVED_DATA_FAILED") && pReady == ("Ready", null), $"upd {pUpd}, fail {pFail}, ready {pReady}");

    // Khoá user bị giữ (đang dựng): Accept chờ rồi 409 busy, không ghi
    var uLock = await SeedCheckInUser(null);
    using (var holderCtx = NewCtx())
    {
        var holder = new UnitOfWork(holderCtx);
        await holder.BeginTransactionAsync();
        await holder.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(uLock), 1000);
        var accLock = await Decide(uLock, true);
        await holder.RollbackTransactionAsync();
        var cntLock = await Counts(uLock);
        Check("G6: dựng lại đang giữ khoá user ⇒ Accept 409 DERIVED_DATA_UPDATING, không ghi", accLock.Reason == "DERIVED_DATA_UPDATING" && cntLock == (0, 1), $"reason {accLock.Reason}, check-ins {cntLock.CheckIns}");
    }

    // 7) Giảm round-trip (L2-L4): KẾT QUẢ giữ nguyên, ít câu SQL hơn
    var sql = new SqlCounter();
    ApplicationDbContext CountedCtx() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(sql).Options);

    // L2: trạng thái cho header/cổng check-in = đúng StatusAsync.State, bằng MỘT truy vấn
    var l2Ok = true; var l2Detail = new List<string>();
    foreach (var (u, expected) in new[] { (uUpd, "Updating"), (uFail, "Failed"), (uReady2, "Ready") })
    {
        using var c = CountedCtx();
        var un = new UnitOfWork(c);
        sql.Reset(); var light = await DerivedDataJobs.OpenStateAsync(un, u, ciOpt, default); var lightCount = sql.Count;
        var full = (await DerivedDataJobs.StatusAsync(un, u, ciOpt, default)).State;
        l2Ok &= light == expected && full == expected && lightCount == 1;
        l2Detail.Add($"{expected}: light {light}/{lightCount} stmt, full {full}");
    }
    Check("L2: OpenStateAsync = StatusAsync.State (Ready/Updating/Failed) bằng 1 câu SQL", l2Ok, string.Join("; ", l2Detail));

    // L3: hồ sơ (múi giờ) đọc MỘT lần mỗi scope; scope mới/Forget thấy thay đổi ngay
    var uLoc = Guid.NewGuid();
    using (var c = NewCtx()) { c.Add(new UserProfile { Id = Guid.NewGuid(), UserId = uLoc, FirstName = "T", LastName = "T", TimeZone = "Asia/Tokyo" }); await c.SaveChangesAsync(); }
    using (var c = CountedCtx())
    {
        var un = new UnitOfWork(c);
        sql.Reset();
        var l1 = await UserLocale.GetAsync(un, uLoc, TimeProvider.System, default);
        var l2 = await UserLocale.GetAsync(un, uLoc, TimeProvider.System, default);
        var l3 = await UserLocale.GetAsync(un, uLoc, TimeProvider.System, default);
        var readsInScope = sql.Count;
        using (var w = NewCtx()) { var p = await w.Set<UserProfile>().FirstAsync(x => x.UserId == uLoc); p.TimeZone = "Pacific/Kiritimati"; await w.SaveChangesAsync(); }
        var stillMemo = await UserLocale.GetAsync(un, uLoc, TimeProvider.System, default);
        UserLocale.Forget(un);
        var afterForget = await UserLocale.GetAsync(un, uLoc, TimeProvider.System, default);
        using var c2 = CountedCtx();
        var freshScope = await UserLocale.GetAsync(new UnitOfWork(c2), uLoc, TimeProvider.System, default);
        Check("L3: 3 lần GetAsync trong 1 scope = 1 câu SQL, cùng kết quả; Forget và scope mới thấy múi giờ mới",
            readsInScope == 1 && l1 == l2 && l2 == l3 && l1.ZoneId == "Asia/Tokyo" && stillMemo.ZoneId == "Asia/Tokyo" && afterForget.ZoneId == "Pacific/Kiritimati" && freshScope.ZoneId == "Pacific/Kiritimati",
            $"statements {readsInScope}, zone {l1.ZoneId} → memo {stillMemo.ZoneId} → forget {afterForget.ZoneId}, new scope {freshScope.ZoneId}");
    }

    // L4: dựng lại từ GIỮA chuỗi cho đúng kết quả của dựng lại từ đầu (TDEE/status ngày trước lấy từ một truy vấn), và không còn truy vấn riêng chỉ lấy Status
    var uEq = Guid.NewGuid();
    var eqStart = ciToday.AddDays(-40);
    using (var c = NewCtx())
    {
        var prog = new MacroProgram { Id = Guid.NewGuid(), UserId = uEq, ProgramType = ProgramType.Coached, GoalType = GoalType.WeightLoss, StartDate = eqStart, ExpenditureStartDate = eqStart, ManualInitialTdeeKcal = 2500, CheckInWeekday = DayOfWeek.Monday, WeeklyWeightChangeTargetPercent = 0.5m, WeeklyWeightChangeTargetKg = -0.4m, ProteinGPerKgBodyweight = 2, IsActive = true };
        prog.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = eqStart, CaloriesTarget = 2000, ProteinTarget = 150, CarbsTarget = 200, FatTarget = 60, AdjustmentReason = AdjustmentReason.Initial });
        c.Add(prog);
        for (var i = 0; i <= 40; i++)
        {
            var d = eqStart.AddDays(i); var logId = Guid.NewGuid(); var kcal = 1900 + (i % 6) * 60;
            c.Add(new FoodLog { Id = logId, UserId = uEq, LogDate = d, TotalCalories = kcal });
            c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = kcal });
            if (i % 2 == 0) c.Add(new WeightEntry { Id = Guid.NewGuid(), UserId = uEq, LogDate = d, WeightKg = 80m - i * 0.03m, TrendWeightKg = 80m - i * 0.03m });
        }
        await c.SaveChangesAsync();
    }
    async Task<string> SnapshotsOf()
    {
        using var c = NewCtx();
        var rows = await c.Set<ExpenditureSnapshot>().Where(s => s.UserId == uEq).OrderBy(s => s.SnapshotDate).ToListAsync();
        return string.Join("|", rows.Select(s => $"{s.SnapshotDate:yyyyMMdd}:{s.EstimatedTdeeKcal}:{s.Status}:{s.AvgCaloriesConsumed}:{s.WeightChangeRateKgPerWeek}:{s.WindowDays}:{s.AlgorithmVersion}"));
    }
    using (var c = NewCtx()) { await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), uEq, eqStart, ciToday, new AlgorithmOptions(), default, null, ciToday); await c.SaveChangesAsync(); }
    var full0 = await SnapshotsOf();
    int rowsFromMiddle; int writes;
    using (var c = CountedCtx())
    {
        sql.Reset();
        var res = await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), uEq, eqStart.AddDays(20), ciToday, new AlgorithmOptions(), default, null, ciToday);
        writes = await c.SaveChangesAsync(); rowsFromMiddle = res.RowsWritten;
    }
    var statusOnly = sql.Texts.Count(t => t.Contains("FROM [ExpenditureSnapshots]") && t.Contains("TOP(1)") && !t.Contains("EstimatedTdeeKcal"));
    var topOne = sql.Texts.Count(t => t.Contains("FROM [ExpenditureSnapshots]") && t.Contains("TOP(1)"));
    var afterMiddle = await SnapshotsOf();
    Check("L4: dựng lại từ giữa chuỗi cho đúng snapshot như dựng từ đầu (không đổi dòng nào), chỉ 1 truy vấn 'snapshot trước' (không còn truy vấn riêng cho Status)",
        full0.Length > 0 && afterMiddle == full0 && writes == 0 && rowsFromMiddle == 0 && topOne == 1 && statusOnly == 0, $"rows {full0.Split('|').Length}, written {rowsFromMiddle}/{writes}, TOP(1) snapshot queries {topOne}, status-only {statusOnly}");

    // 8) G6 chính thức (user duyệt): HTTP 409 cả hai mã, KHÔNG đổi dòng nào khi bị chặn, Failed → Retry → Ready → Accept, và race "job bắt đầu/kết thúc trong lúc Accept"
    // Dấu vân tay mọi bảng Accept có thể ghi (program, tuần, target ngày, snapshot, daily, check-in, goal): COUNT + CHECKSUM_AGG(BINARY_CHECKSUM(*)) theo user; RowVersion đổi khi có UPDATE nên bắt được cả sửa dòng
    async Task<string> Fingerprint(Guid u)
    {
        using var c = NewCtx();
        string T<TE>() => c.Model.FindEntityType(typeof(TE))!.GetTableName()!;
        var progs = $"(SELECT [Id] FROM [{T<MacroProgram>()}] WHERE [UserId] = '{u}')";
        var weeks = $"(SELECT [Id] FROM [{T<MacroProgramWeek>()}] WHERE [MacroProgramId] IN {progs})";
        var parts = new (string Name, string From)[]
        {
            ("programs", $"FROM [{T<MacroProgram>()}] WHERE [UserId] = '{u}'"),
            ("programWeeks", $"FROM [{T<MacroProgramWeek>()}] WHERE [MacroProgramId] IN {progs}"),
            ("weekDayTargets", $"FROM [{T<MacroProgramWeekDayTarget>()}] WHERE [MacroProgramWeekId] IN {weeks}"),
            ("dayTargets", $"FROM [{T<MacroProgramDayTarget>()}] WHERE [MacroProgramId] IN {progs}"),
            ("expenditureSnapshots", $"FROM [{T<ExpenditureSnapshot>()}] WHERE [UserId] = '{u}'"),
            ("dailySnapshots", $"FROM [{T<DailyProgressSnapshot>()}] WHERE [UserId] = '{u}'"),
            ("checkInHistory", $"FROM [{T<WeeklyCheckIn>()}] WHERE [UserId] = '{u}'"),
            ("goals", $"FROM [{T<Goal>()}] WHERE [UserId] = '{u}'"),
        };
        var list = new List<string>();
        foreach (var (name, from) in parts)
            list.Add($"{name}={await c.Database.SqlQueryRaw<string>($"SELECT CONCAT(COUNT(*), ':', CHECKSUM_AGG(BINARY_CHECKSUM(*))) AS [Value] {from}").SingleAsync()}");
        return string.Join(" ", list);
    }
    async Task<(string? Reason, int? Status, Guid? Id)> DecideWith(Guid u, bool accept, AlgorithmOptions o, Func<ApplicationDbContext>? make = null)
    {
        using var c = (make ?? NewCtx)();
        try { return (null, 200, await CheckInSupport.DecideAsync(new UnitOfWork(c), u, ciToday, o, accept, default)); }
        catch (AppException e) { return (e.Errors is { } er && er.TryGetValue("reasonCode", out var v) ? v[0] : e.Message, e.StatusCode, null); }
    }
    // Chứng minh một request đang CHỜ sp_getapplock (yêu cầu khoá chỉ có SAU đường nhanh ngoài khoá) thay vì đoán bằng thời gian; dừng nếu request đã xong
    async Task<bool> WaitForLockWaiter(Task running, int timeoutMs = 5000)
    {
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(cs);
        await conn.OpenAsync();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs && !running.IsCompleted)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sys.dm_tran_locks WHERE resource_type = 'APPLICATION' AND request_status = 'WAIT' AND resource_database_id = DB_ID()";
            if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0) return true;
            await Task.Delay(15);
        }
        return false;
    }
    async Task Hold(Guid u, Func<ApplicationDbContext, UnitOfWork, Task> beforeRelease, Func<Task<(string? Reason, int? Status, Guid? Id)>> accept, Action<(string? Reason, int? Status, Guid? Id), bool> verify)
    {
        using var holderCtx = NewCtx();
        var holder = new UnitOfWork(holderCtx);
        await holder.BeginTransactionAsync();
        if (!await holder.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(u), 1000)) throw new InvalidOperationException("holder could not lock");
        var acceptTask = Task.Run(accept);                  // Accept bắt đầu trong lúc "lần dựng" đang giữ khoá user
        var waitedForLock = await WaitForLockWaiter(acceptTask);   // Accept đang chờ khoá (đã qua đường nhanh), chưa đọc trạng thái
        await beforeRelease(holderCtx, holder);             // "lần dựng" ghi job / snapshot mới
        await holder.CommitTransactionAsync();              // nhả khoá
        verify(await acceptTask, waitedForLock);
    }
    var waitLong = new AlgorithmOptions { DerivedLockWaitMs = 8000 };

    // 8a) Ready: Accept theo validation hiện có; dấu vân tay ĐỔI (đối chứng: phép đo bắt được ghi)
    var uR = await SeedCheckInUser(null);
    var fpR0 = await Fingerprint(uR);
    var readyRes = await DecideWith(uR, true, ciOpt);
    var fpR1 = await Fingerprint(uR);
    Check("G6 8a Ready: Accept 200, có ghi tuần mới + check-in (đối chứng dấu vân tay)", readyRes.Status == 200 && readyRes.Id is not null && fpR0 != fpR1, $"status {readyRes.Status}, fingerprint changed {fpR0 != fpR1}");

    // 8b) Updating: 409 DERIVED_DATA_UPDATING, KHÔNG đổi dòng nào (program, tuần, target ngày, snapshot, daily, check-in, goal)
    var uU = await SeedCheckInUser(JobState.Pending);
    var fpU0 = await Fingerprint(uU);
    var u409 = await DecideWith(uU, true, ciOpt);
    var fpU1 = await Fingerprint(uU);
    Check("G6 8b Updating: HTTP 409 DERIVED_DATA_UPDATING, dấu vân tay mọi bảng không đổi", u409 is { Status: 409, Reason: "DERIVED_DATA_UPDATING" } && fpU0 == fpU1, $"status {u409.Status}, reason {u409.Reason}, unchanged {fpU0 == fpU1}");

    // 8c) Failed: 409 DERIVED_DATA_FAILED, không đổi gì; Retry → job Pending (Updating, vẫn chặn) → worker dựng xong (Ready) → Accept được, không tự Accept
    var uF = await SeedCheckInUser(JobState.Failed);
    var fpF0 = await Fingerprint(uF);
    var f409 = await DecideWith(uF, true, ciOpt);
    var fpF1 = await Fingerprint(uF);
    DerivedDataStatusResponse afterRetry;
    using (var c = NewCtx()) afterRetry = await new RetryDerivedDataCommandHandler(new UnitOfWork(c), new TestUser(uF), Microsoft.Extensions.Options.Options.Create(ciOpt), TimeProvider.System).Handle(new RetryDerivedDataCommand(), default);
    var whileRetrying = await DecideWith(uF, true, ciOpt);
    var checkInsAfterRetry = (await Counts(uF)).CheckIns;
    Guid retryJob;
    using (var c = NewCtx()) retryJob = await c.Set<RecalculationJob>().Where(j => j.UserId == uF && j.State == JobState.Pending).Select(j => j.Id).SingleAsync();
    using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), uF, retryJob, ciOpt, TimeProvider.System, default);   // như worker
    string stateAfterWorker;
    using (var c = NewCtx()) stateAfterWorker = await DerivedDataJobs.OpenStateAsync(new UnitOfWork(c), uF, ciOpt, default);
    var fAccept = await DecideWith(uF, true, ciOpt);
    Check("G6 8c Failed: 409 DERIVED_DATA_FAILED không đổi gì; Retry ⇒ Updating (vẫn 409, không check-in); worker xong ⇒ Ready ⇒ Accept 200",
        f409 is { Status: 409, Reason: "DERIVED_DATA_FAILED" } && fpF0 == fpF1 && afterRetry.State == "Updating" && whileRetrying is { Status: 409, Reason: "DERIVED_DATA_UPDATING" } && checkInsAfterRetry == 0 && stateAfterWorker == "Ready" && fAccept.Status == 200,
        $"blocked {f409.Status}/{f409.Reason}, unchanged {fpF0 == fpF1}, retry {afterRetry.State}, during retry {whileRetrying.Status}/{whileRetrying.Reason}, after worker {stateAfterWorker}, accept {fAccept.Status}");

    // 8d) RACE: Accept bắt đầu khi một lần dựng đang giữ khoá; lần dựng đó xếp job mới rồi nhả khoá ⇒ Accept (đọc trạng thái SAU khi có khoá) thấy job ⇒ 409, không ghi gì
    var uJ = await SeedCheckInUser(null);
    var fpJ0 = await Fingerprint(uJ);
    await Hold(uJ,
        async (c, _) => { c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uJ, Cause = RecalculationCause.FoodLogChanged, FromDate = ciToday, ToDate = ciToday, State = JobState.Pending }); await c.SaveChangesAsync(); },
        () => DecideWith(uJ, true, waitLong),
        (res, waited) =>
        {
            var fpJ1 = Fingerprint(uJ).GetAwaiter().GetResult();
            Check("G6 8d race: job xuất hiện trong lúc Accept chờ khoá ⇒ 409 DERIVED_DATA_UPDATING, không đổi dòng nào", waited && res is { Status: 409, Reason: "DERIVED_DATA_UPDATING" } && fpJ0 == fpJ1, $"accept was waiting {waited}, status {res.Status}, reason {res.Reason}, unchanged {fpJ0 == fpJ1}");
        });

    // 8e) RACE: Accept bắt đầu khi lần dựng đang giữ khoá; lần dựng XONG (TDEE mới 2650, không còn job) rồi nhả khoá ⇒ Accept dùng TDEE MỚI, không dùng số cũ đã thấy trước khi chờ
    var uD = await SeedCheckInUser(null);
    await Hold(uD,
        async (c, _) => { var s = await c.Set<ExpenditureSnapshot>().FirstAsync(x => x.UserId == uD && x.SnapshotDate == ciToday); s.EstimatedTdeeKcal = 2650; await c.SaveChangesAsync(); },
        () => DecideWith(uD, true, waitLong),
        (res, waited) =>
        {
            using var c = NewCtx();
            var ci = c.Set<WeeklyCheckIn>().Where(x => x.UserId == uD).ToList();
            Check("G6 8e race: lần dựng xong trong lúc Accept chờ khoá ⇒ Accept 200 với TDEE mới (2650)", waited && res.Status == 200 && ci.Count == 1 && ci[0].TdeeEstimate == 2650, $"accept was waiting {waited}, status {res.Status}, tdee stored {ci.FirstOrDefault()?.TdeeEstimate}");
        });

    // 8f) Khoá bị giữ quá thời gian chờ ⇒ 409 busy, không đổi dòng nào
    var uB = await SeedCheckInUser(null);
    var fpB0 = await Fingerprint(uB);
    await Hold(uB, async (_, _) => await Task.Delay(1200), () => DecideWith(uB, true, ciOpt),   // ciOpt: chờ 300 ms < giữ ≈ 1.9 s
        (res, _) =>
        {
            var fpB1 = Fingerprint(uB).GetAwaiter().GetResult();
            Check("G6 8f: lần dựng giữ khoá lâu hơn thời gian chờ ⇒ 409 DERIVED_DATA_UPDATING, không đổi dòng nào", res is { Status: 409, Reason: "DERIVED_DATA_UPDATING" } && fpB0 == fpB1, $"status {res.Status}, reason {res.Reason}, unchanged {fpB0 == fpB1}");
        });

    // 8g) Preview báo trước để FE tắt nút: Ready/Updating/Failed
    var pv = (await PreviewOf(uR), await PreviewOf(uU));
    Check("G6 8g preview: derivedDataState/acceptBlockedReason cho FE tắt nút trước", pv.Item2 == ("Updating", "DERIVED_DATA_UPDATING") && pv.Item1.Item1 is "Ready" or "Updating", $"ready-user {pv.Item1}, updating-user {pv.Item2}");

    // 9) Rà soát độc lập: (a-c) Accept trùng đồng thời phải idempotent; (d) ngày có snapshot phiên bản MỚI HƠN vẫn tôn trọng ngân sách dòng của lần dựng đồng bộ
    // 9a) Request 1 ghi check-in Accept trong lúc request 2 đã qua kiểm tra "đã quyết định hôm nay" (ngoài khoá) và đang chờ khoá ⇒ request 2 trả đúng Id của request 1, không ghi thêm
    var uDup = await SeedCheckInUser(null);
    var dupId = Guid.NewGuid();
    await Hold(uDup,
        async (c, _) =>
        {
            // kèm một job HỎNG hết lượt (rà soát vòng 3): nếu lần đọc lại quyết định dưới khoá bị mất/đặt sau cổng thì cổng chặn bằng 409 DERIVED_DATA_FAILED — mã mà nhánh
            // đọc-lại-khi-409 (chỉ cho DERIVED_DATA_UPDATING) không cứu ⇒ chỉ lần đọc lại dưới khoá mới cho ra 200 + Id
            c.Add(new WeeklyCheckIn { Id = dupId, UserId = uDup, CheckInDate = ciToday, MacroProgramWeekId = Guid.NewGuid(), TdeeEstimate = 2500, Accepted = true });
            c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uDup, Cause = RecalculationCause.FoodLogChanged, FromDate = ciToday, ToDate = ciToday, State = JobState.Failed, AttemptCount = 5 });
            await c.SaveChangesAsync();
        },
        () => DecideWith(uDup, true, waitLong),
        (res, waited) =>
        {
            var cnt = Counts(uDup).GetAwaiter().GetResult();
            Check("9a Accept trùng: request chờ khoá thấy check-in Accept vừa ghi ⇒ 200 + Id cũ, không ghi thêm", waited && res.Status == 200 && res.Id == dupId && cnt == (1, 1), $"waited {waited}, status {res.Status}, same id {res.Id == dupId}, reason {res.Reason ?? "-"}, check-ins {cnt.CheckIns}, weeks {cnt.Weeks}");
        });

    // 9b) Quyết định đã ghi trong lúc chờ là Reject ⇒ Accept trả 409 CheckInDecisionConflict (không phải CheckInNotDue / DERIVED_DATA_UPDATING), không ghi thêm
    var uDup2 = await SeedCheckInUser(null);
    await Hold(uDup2,
        async (c, _) =>
        {
            c.Add(new WeeklyCheckIn { Id = Guid.NewGuid(), UserId = uDup2, CheckInDate = ciToday, MacroProgramWeekId = Guid.NewGuid(), TdeeEstimate = 2500, Accepted = false });
            c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uDup2, Cause = RecalculationCause.FoodLogChanged, FromDate = ciToday, ToDate = ciToday, State = JobState.Failed, AttemptCount = 5 });   // như 9a
            await c.SaveChangesAsync();
        },
        () => DecideWith(uDup2, true, waitLong),
        (res, waited) =>
        {
            var cnt = Counts(uDup2).GetAwaiter().GetResult();
            Check("9b Accept sau Reject ghi trong lúc chờ khoá ⇒ 409 CheckInDecisionConflict, không ghi thêm", waited && res.Status == 409 && res.Reason == ExceptionMessages.CheckInDecisionConflict && cnt == (1, 1), $"waited {waited}, status {res.Status}, reason {res.Reason}, check-ins {cnt.CheckIns}, weeks {cnt.Weeks}");
        });

    // 9c) Hai Accept thật cùng lúc (hai tab) ⇒ cả hai 200 cùng một Id, đúng 1 check-in + 1 tuần mới
    var uTwo = await SeedCheckInUser(null);
    var both = await Task.WhenAll(Task.Run(() => DecideWith(uTwo, true, waitLong)), Task.Run(() => DecideWith(uTwo, true, waitLong)));
    var cntTwo = await Counts(uTwo);
    Check("9c hai Accept đồng thời ⇒ cả hai 200 cùng Id, 1 check-in, 1 tuần mới", both.All(x => x.Status == 200) && both[0].Id == both[1].Id && cntTwo == (1, 2), $"statuses {both[0].Status}/{both[1].Status} reasons {both[0].Reason ?? "-"}/{both[1].Reason ?? "-"}, same id {both[0].Id == both[1].Id}, check-ins {cntTwo.CheckIns}, weeks {cntTwo.Weeks}");

    // 9c') Rà soát vòng 1: Reject không lấy khoá ⇒ hai Reject song song, hoặc Accept song song Reject (hai tab), từng ra 500 (unique) hoặc 409 CheckInNotDue.
    //       Mong đợi: cùng quyết định ⇒ cả hai 200 cùng Id; khác quyết định ⇒ đúng một 200 + một 409 CheckInDecisionConflict; luôn đúng 1 check-in, Accepted lưu đúng bên thắng. 8 vòng mỗi loại.
    async Task<(string? Reason, int? Status, Guid? Id)> DecideAny(Guid u, bool accept)
    {
        try { return await DecideWith(u, accept, waitLong); }
        catch (Exception e) { return ($"{e.GetType().Name}: {(e.InnerException ?? e).Message[..Math.Min(80, (e.InnerException ?? e).Message.Length)]}", 500, null); }
    }
    var raceProblems = new List<string>();
    for (var round = 0; round < 8; round++)
    {
        var uRR = await SeedCheckInUser(null);
        var rr = await Task.WhenAll(Task.Run(() => DecideAny(uRR, false)), Task.Run(() => DecideAny(uRR, false)));
        var cRR = await Counts(uRR);
        bool? accRR; using (var c = NewCtx()) accRR = await c.Set<WeeklyCheckIn>().Where(x => x.UserId == uRR).Select(x => (bool?)x.Accepted).SingleOrDefaultAsync();
        if (!(rr.All(x => x.Status == 200) && rr[0].Id == rr[1].Id && cRR == (1, 2) && accRR == false)) raceProblems.Add($"R+R#{round}: {rr[0].Status}/{rr[1].Status} {rr[0].Reason ?? "-"} | {rr[1].Reason ?? "-"} checkins {cRR.CheckIns} weeks {cRR.Weeks}");

        var uAR = await SeedCheckInUser(null);
        var ar = await Task.WhenAll(Task.Run(() => DecideAny(uAR, true)), Task.Run(() => DecideAny(uAR, false)));
        var cAR = await Counts(uAR);
        bool? accAR; using (var c = NewCtx()) accAR = await c.Set<WeeklyCheckIn>().Where(x => x.UserId == uAR).Select(x => (bool?)x.Accepted).SingleOrDefaultAsync();
        var winner = ar[0].Status == 200 ? true : ar[1].Status == 200 ? false : (bool?)null;
        var loser = ar[0].Status == 200 ? ar[1] : ar[0];
        var okAR = ar.Count(x => x.Status == 200) == 1 && loser is { Status: 409 } && loser.Reason == ExceptionMessages.CheckInDecisionConflict && cAR == (1, 2) && accAR == winner;
        if (!okAR) raceProblems.Add($"A+R#{round}: accept {ar[0].Status} {ar[0].Reason ?? "-"} | reject {ar[1].Status} {ar[1].Reason ?? "-"} checkins {cAR.CheckIns} weeks {cAR.Weeks} stored {accAR?.ToString() ?? "null"}");
    }
    Check("9c' Reject+Reject và Accept+Reject song song (8 vòng mỗi loại): không 500, không 409 sai mã, 1 check-in, Accepted đúng bên thắng", raceProblems.Count == 0, raceProblems.Count == 0 ? "16 races ok" : string.Join(" || ", raceProblems));

    // 9g) Rà soát vòng 2: ép TẤT ĐỊNH từng nhánh phục hồi (không dựa vào may rủi thời điểm của 9c'): quyết định đối thủ được commit
    //     (i) NGAY TRƯỚC câu đọc lastCheckIn của bên thua ⇒ nhánh AlreadyCheckedInToday; (ii) NGAY TRƯỚC SaveChanges của bên thua ⇒ nhánh DbUpdateException (unique). Mỗi ca kiểm hook đã chạy.
    //     (Accept thắng + Accept thua không ép được: bên thua giữ khoá user trong lúc hook chạy; ca đó do 9a phủ.)
    async Task<string?> Forced(string name, bool loserAccept, bool winnerAccept, bool beforeSave)
    {
        var u = await SeedCheckInUser(null);
        (string? Reason, int? Status, Guid? Id) win = default;
        Func<Task> competitor = async () => win = await DecideWith(u, winnerAccept, waitLong);
        var readHook = new BeforeReader(cmd => cmd.CommandText.Contains("[WeeklyCheckIns]") && cmd.CommandText.Contains("ORDER BY"), competitor);
        var saveHook = new BeforeSave(competitor);
        ApplicationDbContext MakeLoser() => beforeSave
            ? new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(saveHook).Options)
            : new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(readHook).Options);
        var lose = await DecideWith(u, loserAccept, waitLong, MakeLoser);
        var fired = beforeSave ? saveHook.Fired : readHook.Fired;
        var cnt = await Counts(u);
        bool? stored; using (var c = NewCtx()) stored = await c.Set<WeeklyCheckIn>().Where(x => x.UserId == u).Select(x => (bool?)x.Accepted).SingleOrDefaultAsync();
        var loserOk = loserAccept == winnerAccept ? lose.Status == 200 && lose.Id == win.Id : lose is { Status: 409 } && lose.Reason == ExceptionMessages.CheckInDecisionConflict;
        return fired && win.Status == 200 && loserOk && cnt == (1, 2) && stored == winnerAccept ? null
            : $"{name}: hook fired {fired}, winner {win.Status} {win.Reason ?? "-"}, loser {lose.Status} {lose.Reason ?? "-"}, same id {lose.Id == win.Id}, check-ins {cnt.CheckIns}, weeks {cnt.Weeks}, stored {stored?.ToString() ?? "null"}";
    }
    var forcedProblems = new List<string?>
    {
        await Forced("R thua/R thắng, trước lastCheckIn", false, false, false),
        await Forced("R thua/R thắng, trước SaveChanges", false, false, true),
        await Forced("A thua/R thắng, trước lastCheckIn (dưới khoá)", true, false, false),
        await Forced("A thua/R thắng, trước SaveChanges (dưới khoá)", true, false, true),
        await Forced("R thua/A thắng, trước lastCheckIn", false, true, false),
        await Forced("R thua/A thắng, trước SaveChanges", false, true, true),
    }.Where(p => p is not null).ToList();
    Check("9g ép tất định 6 ca đua quyết định: nhánh AlreadyCheckedInToday và nhánh DbUpdateException đều trả Id / 409 CheckInDecisionConflict, 1 check-in, Accepted đúng bên thắng",
        forcedProblems.Count == 0, forcedProblems.Count == 0 ? "6/6 forced interleavings ok, every hook fired" : string.Join(" || ", forcedProblems));

    // 9h) Rà soát vòng 2: Accept ở tab 2 hết thời gian chờ khoá (tab 1 giữ khoá lâu trên DB xa) nhưng quyết định của tab 1 đã ghi ⇒ trả Id đó (trước: 409 DERIVED_DATA_UPDATING gây hiểu lầm)
    var uBusy = await SeedCheckInUser(null);
    var busyId = Guid.NewGuid();
    using (var holderCtx = NewCtx())
    {
        var holder = new UnitOfWork(holderCtx);
        await holder.BeginTransactionAsync();
        await holder.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(uBusy), 1000);
        var tab2 = Task.Run(() => DecideWith(uBusy, true, new AlgorithmOptions { DerivedLockWaitMs = 1500 }));
        var tab2Waiting = await WaitForLockWaiter(tab2);
        using (var w = NewCtx()) { w.Add(new WeeklyCheckIn { Id = busyId, UserId = uBusy, CheckInDate = ciToday, MacroProgramWeekId = Guid.NewGuid(), TdeeEstimate = 2500, Accepted = true }); await w.SaveChangesAsync(); }   // quyết định "tab 1" đã commit, khoá vẫn bị giữ
        var r2 = await tab2;   // hết 1.5 s chờ khoá
        await holder.RollbackTransactionAsync();
        var cntBusy = await Counts(uBusy);
        Check("9h Accept hết thời gian chờ khoá nhưng tab khác đã ghi quyết định ⇒ 200 + Id đó (không phải 409 DERIVED_DATA_UPDATING), không ghi thêm",
            tab2Waiting && r2.Status == 200 && r2.Id == busyId && cntBusy == (1, 1), $"waiting {tab2Waiting}, status {r2.Status}, reason {r2.Reason ?? "-"}, same id {r2.Id == busyId}, check-ins {cntBusy.CheckIns}, weeks {cntBusy.Weeks}");
    }

    // 9i) Rà soát vòng 2 (lỗi có sẵn): sửa log ăn bị xếp job (khoá bận/lỗi) gộp vào job Pending của nguyên nhân khác (sửa cân) ⇒ job phải mang FoodLogChanged để worker làm mới aggregate ngày ăn đó
    var uMerge = Guid.NewGuid();
    var mergeDay = ciToday.AddDays(-2);
    using (var c = NewCtx())
    {
        var logId = Guid.NewGuid();
        c.Add(new FoodLog { Id = logId, UserId = uMerge, LogDate = mergeDay, TotalCalories = 2100 });
        c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = 2100 });
        c.Add(new NutritionDayAggregate { Id = Guid.NewGuid(), UserId = uMerge, LocalDate = mergeDay, Calories = 2000, NutrientsJson = "{\"energy_kcal\":2000}" });   // aggregate cũ (trước lần sửa)
        c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uMerge, Cause = RecalculationCause.WeightChanged, FromDate = ciToday.AddDays(-10), ToDate = ciToday.AddDays(-10), State = JobState.Pending });
        await c.SaveChangesAsync();
    }
    using (var c = NewCtx()) await DerivedDataJobs.EnqueueAsync(new UnitOfWork(c), uMerge, mergeDay, mergeDay, RecalculationCause.FoodLogChanged, "lock busy", default);
    List<RecalculationJob> mergedJobs; using (var c = NewCtx()) mergedJobs = await c.Set<RecalculationJob>().Where(j => j.UserId == uMerge && j.State == JobState.Pending).ToListAsync();
    using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), uMerge, mergedJobs[0].Id, older, TimeProvider.System, default);
    decimal aggAfter; using (var c = NewCtx()) aggAfter = await c.Set<NutritionDayAggregate>().Where(a => a.UserId == uMerge && a.LocalDate == mergeDay).Select(a => a.Calories).SingleAsync();
    string mergeState; using (var c = NewCtx()) mergeState = await DerivedDataJobs.OpenStateAsync(new UnitOfWork(c), uMerge, older, default);
    Check("9i job Pending gộp từ sửa cân + sửa log ăn ⇒ 1 job mang FoodLogChanged; worker làm mới aggregate ngày ăn (2000 → 2100) rồi Ready",
        mergedJobs.Count == 1 && mergedJobs[0].Cause == RecalculationCause.FoodLogChanged && mergedJobs[0].FromDate == ciToday.AddDays(-10) && mergedJobs[0].ToDate == mergeDay && aggAfter == 2100 && mergeState == "Ready",
        $"pending jobs {mergedJobs.Count}, cause {mergedJobs.FirstOrDefault()?.Cause}, range {mergedJobs.FirstOrDefault()?.FromDate:yyyy-MM-dd}..{mergedJobs.FirstOrDefault()?.ToDate:yyyy-MM-dd}, aggregate {aggAfter}, state {mergeState}");

    // Dữ liệu chung cho 9i'/9j/9k: một ngày ăn đã sửa (log 2100 kcal) mà aggregate còn số cũ (2000)
    async Task SeedStaleFoodDay(Guid u, DateOnly day)
    {
        using var c = NewCtx();
        var logId = Guid.NewGuid();
        c.Add(new FoodLog { Id = logId, UserId = u, LogDate = day, TotalCalories = 2100 });
        c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = 2100 });
        c.Add(new NutritionDayAggregate { Id = Guid.NewGuid(), UserId = u, LocalDate = day, Calories = 2000, NutrientsJson = "{\"energy_kcal\":2000}" });
        await c.SaveChangesAsync();
    }
    async Task<decimal> AggregateOf(Guid u, DateOnly day) { using var c = NewCtx(); return await c.Set<NutritionDayAggregate>().Where(a => a.UserId == u && a.LocalDate == day).Select(a => a.Calories).SingleAsync(); }
    async Task<string> StateOf(Guid u) { using var c = NewCtx(); return await DerivedDataJobs.OpenStateAsync(new UnitOfWork(c), u, older, default); }

    // 9i') Rà soát vòng 3: job gộp bắt đầu SỚM hơn ngày ăn hơn 400 ngày (sửa một lần cân rất cũ) ⇒ vẫn làm mới aggregate của ngày ăn (trước: chỉ 400 ngày đầu của khoảng)
    var uFar = Guid.NewGuid();
    var farDay = ciToday.AddDays(-1);
    await SeedStaleFoodDay(uFar, farDay);
    using (var c = NewCtx()) { c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uFar, Cause = RecalculationCause.WeightChanged, FromDate = farDay.AddDays(-500), ToDate = farDay.AddDays(-500), State = JobState.Pending }); await c.SaveChangesAsync(); }
    using (var c = NewCtx()) await DerivedDataJobs.EnqueueAsync(new UnitOfWork(c), uFar, farDay, farDay, RecalculationCause.FoodLogChanged, "lock busy", default);
    Guid farJob; using (var c = NewCtx()) farJob = await c.Set<RecalculationJob>().Where(j => j.UserId == uFar && j.State == JobState.Pending).Select(j => j.Id).SingleAsync();
    using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), uFar, farJob, older, TimeProvider.System, default);
    var farAgg = await AggregateOf(uFar, farDay); var farState = await StateOf(uFar);
    Check("9i' job gộp dài 501 ngày (sửa cân 500 ngày trước + sửa log ăn hôm qua) ⇒ worker vẫn làm mới aggregate của ngày ăn ở cuối khoảng",
        farAgg == 2100 && farState == "Ready", $"aggregate {farAgg} (2000 = bỏ sót), state {farState}");

    // 9j) Rà soát vòng 3: job sửa log ăn hỏng hết lượt ⇒ người dùng bấm Retry ⇒ job thử lại phải mang FoodLogChanged (trước: Manual ⇒ aggregate không bao giờ được làm mới mà vẫn Ready)
    var uRet = Guid.NewGuid();
    var retDay = ciToday.AddDays(-4);
    await SeedStaleFoodDay(uRet, retDay);
    using (var c = NewCtx()) { c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uRet, Cause = RecalculationCause.FoodLogChanged, FromDate = retDay, ToDate = retDay, State = JobState.Failed, AttemptCount = 5, LastError = "boom" }); await c.SaveChangesAsync(); }
    DerivedDataStatusResponse retStatus;
    using (var c = NewCtx()) retStatus = await new RetryDerivedDataCommandHandler(new UnitOfWork(c), new TestUser(uRet), Microsoft.Extensions.Options.Options.Create(older), TimeProvider.System).Handle(new RetryDerivedDataCommand(), default);
    RecalculationJob retJob; using (var c = NewCtx()) retJob = await c.Set<RecalculationJob>().Where(j => j.UserId == uRet && j.State == JobState.Pending).SingleAsync();
    using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), uRet, retJob.Id, older, TimeProvider.System, default);
    var retAgg = await AggregateOf(uRet, retDay); var retState = await StateOf(uRet);
    Check("9j Retry job sửa log ăn đã hỏng ⇒ job thử lại mang FoodLogChanged ⇒ worker làm mới aggregate (2000 → 2100) rồi Ready",
        retStatus.State == "Updating" && retJob.Cause == RecalculationCause.FoodLogChanged && retAgg == 2100 && retState == "Ready",
        $"after retry {retStatus.State}, retry job cause {retJob.Cause}, aggregate {retAgg}, state {retState}");

    // 9k) Rà soát vòng 3: worker đang chạy đã đọc job Pending P vào lần dựng của nó; ngay trước khi worker lưu, một sửa log ăn bị xếp job gộp vào P ⇒ P phải Ở LẠI Pending
    //     (worker không được đánh dấu xong phần nó chưa làm) và lượt sau làm mới aggregate. Ép tất định bằng hook SaveChanges trên context của worker.
    var uAbs = Guid.NewGuid();
    var absDay = ciToday.AddDays(-3);
    await SeedStaleFoodDay(uAbs, absDay);
    var claimedId = Guid.NewGuid(); var absorbedId = Guid.NewGuid();
    using (var c = NewCtx())
    {
        c.Add(new RecalculationJob { Id = claimedId, UserId = uAbs, Cause = RecalculationCause.Manual, FromDate = ciToday, ToDate = ciToday, State = JobState.Running, AttemptCount = 1, StartedAt = DateTimeOffset.UtcNow });   // job worker đã nhận
        c.Add(new RecalculationJob { Id = absorbedId, UserId = uAbs, Cause = RecalculationCause.WeightChanged, FromDate = ciToday.AddDays(-5), ToDate = ciToday.AddDays(-5), State = JobState.Pending });
        await c.SaveChangesAsync();
    }
    var lateMerge = new BeforeSave(async () => { using var o = NewCtx(); await DerivedDataJobs.EnqueueAsync(new UnitOfWork(o), uAbs, absDay, absDay, RecalculationCause.FoodLogChanged, "lock busy", default); });
    using (var w = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(lateMerge).Options))
        await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(w), uAbs, claimedId, older, TimeProvider.System, default);
    RecalculationJob absAfter; using (var c = NewCtx()) absAfter = await c.Set<RecalculationJob>().SingleAsync(j => j.Id == absorbedId);
    JobState claimedState; using (var c = NewCtx()) claimedState = await c.Set<RecalculationJob>().Where(j => j.Id == claimedId).Select(j => j.State).SingleAsync();
    var midState = await StateOf(uAbs); var midAgg = await AggregateOf(uAbs, absDay);
    using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), uAbs, absorbedId, older, TimeProvider.System, default);   // lượt sau của worker
    var absAgg = await AggregateOf(uAbs, absDay); var absState = await StateOf(uAbs);
    Check("9k job Pending bị gộp thêm trong lúc worker đang chạy ⇒ không bị đánh dấu xong (Pending, Updating); lượt sau làm mới aggregate (2000 → 2100) rồi Ready",
        lateMerge.Fired && claimedState == JobState.Succeeded && absAfter.State == JobState.Pending && absAfter.Cause == RecalculationCause.FoodLogChanged && absAfter.ToDate == absDay
            && midState == "Updating" && midAgg == 2000 && absAgg == 2100 && absState == "Ready",
        $"hook {lateMerge.Fired}, claimed {claimedState}, absorbed {absAfter.State}/{absAfter.Cause} {absAfter.FromDate:yyyy-MM-dd}..{absAfter.ToDate:yyyy-MM-dd}, between runs {midState} agg {midAgg}, after next run agg {absAgg} {absState}");

    // 9l) Rà soát vòng 4: các thứ tự đua còn lại của hàng đợi job, mỗi ca ép TẤT ĐỊNH bằng interceptor
    ApplicationDbContext Hooked(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] i) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(i).Options);
    RecalculationJob NewJob(JobState state, RecalculationCause cause, DateOnly from, DateOnly to, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(), Cause = cause, FromDate = from, ToDate = to, State = state,
        AttemptCount = state == JobState.Running ? 1 : 0, StartedAt = state == JobState.Running ? DateTimeOffset.UtcNow : null,
    };
    async Task SeedJobs(Guid u, params RecalculationJob[] js) { using var c = NewCtx(); foreach (var j in js) { j.UserId = u; c.Add(j); } await c.SaveChangesAsync(); }
    async Task<List<RecalculationJob>> JobsOf(Guid u) { using var c = NewCtx(); return await c.Set<RecalculationJob>().Where(j => j.UserId == u).OrderBy(j => j.FromDate).ThenBy(j => j.ToDate).ToListAsync(); }
    async Task Run(Guid u, Guid jobId, ApplicationDbContext? ctx = null) { using var c = ctx ?? NewCtx(); await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), u, jobId, older, TimeProvider.System, default); }
    async Task Enqueue(Guid u, DateOnly from, DateOnly to, RecalculationCause cause, ApplicationDbContext? ctx = null) { using var c = ctx ?? NewCtx(); await DerivedDataJobs.EnqueueAsync(new UnitOfWork(c), u, from, to, cause, "lock busy", default); }
    bool IsJobUpdate(System.Data.Common.DbCommand cmd) => cmd.CommandText.Contains("UPDATE") && cmd.CommandText.Contains("[RecalculationJobs]");
    var queueProblems = new List<string>();

    // (i) đối chứng dương: job đã nhận + 2 job Pending không bị đụng (ModifiedAt null và khác null) ⇒ cả ba được đánh dấu xong, Ready
    {
        var u = Guid.NewGuid(); var claimed = Guid.NewGuid();
        var touchedEarlier = NewJob(JobState.Pending, RecalculationCause.Manual, ciToday.AddDays(-9), ciToday.AddDays(-8)); touchedEarlier.ModifiedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await SeedJobs(u, NewJob(JobState.Running, RecalculationCause.Manual, ciToday, ciToday, claimed), NewJob(JobState.Pending, RecalculationCause.WeightChanged, ciToday.AddDays(-6), ciToday.AddDays(-6)), touchedEarlier);
        await Run(u, claimed);
        var js = await JobsOf(u); var st = await StateOf(u);
        if (!(js.Count == 3 && js.All(j => j.State == JobState.Succeeded) && st == "Ready")) queueProblems.Add($"(i) control: [{string.Join(",", js.Select(j => j.State))}] state {st}");
    }

    // (ii) gộp thêm CHỈ đổi ModifiedAt (cùng khoảng, cùng FoodLogChanged; log ngày đó vừa sửa 2100 → 2200 sau khi worker đã đọc) ⇒ job ở lại Pending; lượt sau ra 2200
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-3); await SeedStaleFoodDay(u, day);
        var claimed = Guid.NewGuid(); var absorbed = Guid.NewGuid();
        await SeedJobs(u, NewJob(JobState.Running, RecalculationCause.Manual, ciToday, ciToday, claimed), NewJob(JobState.Pending, RecalculationCause.FoodLogChanged, day, day, absorbed));
        var hook = new BeforeSave(async () =>
        {
            using var o = NewCtx();
            var log = await o.Set<FoodLog>().FirstAsync(l => l.UserId == u && l.LogDate == day); log.TotalCalories = 2200;
            (await o.Set<FoodLogEntry>().FirstAsync(x => x.FoodLogId == log.Id)).Calories = 2200;
            await o.SaveChangesAsync();
            await DerivedDataJobs.EnqueueAsync(new UnitOfWork(o), u, day, day, RecalculationCause.FoodLogChanged, "lock busy", default);
        });
        await Run(u, claimed, Hooked(hook));
        var mid = (await JobsOf(u)).Single(j => j.Id == absorbed); var midSt = await StateOf(u);
        await Run(u, absorbed);
        var agg = await AggregateOf(u, day); var fin = await StateOf(u);
        if (!(hook.Fired && mid.State == JobState.Pending && mid.FromDate == day && mid.ToDate == day && midSt == "Updating" && agg == 2200 && fin == "Ready"))
            queueProblems.Add($"(ii) ModifiedAt-only merge: hook {hook.Fired}, absorbed {mid.State} {mid.FromDate:MM-dd}..{mid.ToDate:MM-dd}, between {midSt}, final agg {agg} {fin}");
    }

    // (iii) worker đánh dấu xong job P GIỮA lúc bên xếp job đọc P và lúc UPDATE ⇒ UPDATE 0 dòng ⇒ tạo job Pending mới cho sửa log ăn (không rơi vào dòng đã Succeeded)
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-3); await SeedStaleFoodDay(u, day);
        var claimed = Guid.NewGuid(); var absorbed = Guid.NewGuid();
        await SeedJobs(u, NewJob(JobState.Running, RecalculationCause.Manual, ciToday, ciToday, claimed), NewJob(JobState.Pending, RecalculationCause.WeightChanged, ciToday.AddDays(-5), ciToday.AddDays(-5), absorbed));
        var hook = new BeforeCommand(IsJobUpdate, () => Run(u, claimed));
        await Enqueue(u, day, day, RecalculationCause.FoodLogChanged, Hooked(hook));
        var js = await JobsOf(u); var midSt = await StateOf(u);
        var fresh = js.Where(j => j.State == JobState.Pending).ToList();
        if (fresh.Count == 1) await Run(u, fresh[0].Id);
        var agg = await AggregateOf(u, day); var fin = await StateOf(u);
        if (!(hook.Fired && js.Single(j => j.Id == absorbed).State == JobState.Succeeded && fresh.Count == 1 && fresh[0].Id != absorbed && fresh[0].Cause == RecalculationCause.FoodLogChanged
              && fresh[0].FromDate == day && fresh[0].ToDate == day && midSt == "Updating" && agg == 2100 && fin == "Ready"))
            queueProblems.Add($"(iii) worker finished between read and update: hook {hook.Fired}, jobs [{string.Join(", ", js.Select(j => $"{j.State}/{j.Cause} {j.FromDate:MM-dd}..{j.ToDate:MM-dd}"))}], between {midSt}, final agg {agg} {fin}");
    }

    // (iv) HAI lần xếp job cùng đọc P trước khi bên nào UPDATE (A: sửa log ăn -40; B: sửa cân -1, commit ngay trước UPDATE của A) ⇒ P = hợp của CẢ HAI, FoodLogChanged
    {
        var u = Guid.NewGuid();
        await SeedJobs(u, NewJob(JobState.Pending, RecalculationCause.WeightChanged, ciToday.AddDays(-3), ciToday.AddDays(-3)));
        var hook = new BeforeCommand(IsJobUpdate, () => Enqueue(u, ciToday.AddDays(-1), ciToday.AddDays(-1), RecalculationCause.WeightChanged));
        await Enqueue(u, ciToday.AddDays(-40), ciToday.AddDays(-40), RecalculationCause.FoodLogChanged, Hooked(hook));
        var js = await JobsOf(u);
        if (!(hook.Fired && js.Count == 1 && js[0].State == JobState.Pending && js[0].FromDate == ciToday.AddDays(-40) && js[0].ToDate == ciToday.AddDays(-1) && js[0].Cause == RecalculationCause.FoodLogChanged))
            queueProblems.Add($"(iv) concurrent merges: hook {hook.Fired}, jobs [{string.Join(", ", js.Select(j => $"{j.State}/{j.Cause} {j.FromDate:yyyy-MM-dd}..{j.ToDate:yyyy-MM-dd}"))}] (expected {ciToday.AddDays(-40):yyyy-MM-dd}..{ciToday.AddDays(-1):yyyy-MM-dd} FoodLogChanged)");
    }

    // (v) worker A chạy quá lease: worker B giành lại job, Busy ⇒ trả về Pending, rồi một sửa log ăn gộp vào ⇒ A KHÔNG được đánh dấu xong job đó; lượt sau làm mới aggregate
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-3); await SeedStaleFoodDay(u, day);
        var job = Guid.NewGuid();
        await SeedJobs(u, NewJob(JobState.Running, RecalculationCause.Manual, ciToday, ciToday, job));
        var hook = new BeforeSave(async () =>
        {
            using var o = NewCtx();   // như DerivedDataWorker.ProcessAsync (giành lại khi quá lease) rồi ReleaseAsync (Busy)
            await o.Set<RecalculationJob>().Where(x => x.Id == job).ExecuteUpdateAsync(s => s.SetProperty(x => x.StartedAt, DateTimeOffset.UtcNow.AddSeconds(1)).SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1));
            await o.Set<RecalculationJob>().Where(x => x.Id == job && x.State == JobState.Running).ExecuteUpdateAsync(s => s.SetProperty(x => x.State, JobState.Pending).SetProperty(x => x.AttemptCount, x => x.AttemptCount - 1));
            await DerivedDataJobs.EnqueueAsync(new UnitOfWork(o), u, day, day, RecalculationCause.FoodLogChanged, "lock busy", default);
        });
        await Run(u, job, Hooked(hook));
        var after = (await JobsOf(u)).Single(); var midSt = await StateOf(u);
        await Run(u, after.Id);
        var agg = await AggregateOf(u, day); var fin = await StateOf(u);
        if (!(hook.Fired && after.State == JobState.Pending && after.Cause == RecalculationCause.FoodLogChanged && after.FromDate == day && midSt == "Updating" && agg == 2100 && fin == "Ready"))
            queueProblems.Add($"(v) lease steal: hook {hook.Fired}, job {after.State}/{after.Cause} {after.FromDate:MM-dd}..{after.ToDate:MM-dd}, between {midSt}, final agg {agg} {fin}");
    }

    // (vi) hai job Pending cùng ngày bắt đầu: E d-18..d-16 (xếp TRƯỚC theo Id), Q d-18..d-10; sửa log ăn d-13..d-10 ⇒ gộp vào Q (đã phủ, khoảng không đổi).
    //      Gộp vào job xếp đầu (E) sẽ ra đúng d-18..d-10 = khoảng của Q ⇒ vi phạm unique (Pending) ⇒ 500 sau khi nguồn đã lưu
    {
        var u = Guid.NewGuid(); var a = ciToday.AddDays(-18);
        Guid Tail(string t) => Guid.Parse(Guid.NewGuid().ToString()[..24] + t);   // SQL Server so uniqueidentifier theo 6 byte cuối trước ⇒ ...01 xếp trước ...02
        var e = NewJob(JobState.Pending, RecalculationCause.Manual, a, ciToday.AddDays(-16), Tail("000000000001"));
        var q = NewJob(JobState.Pending, RecalculationCause.WeightChanged, a, ciToday.AddDays(-10), Tail("000000000002"));
        await SeedJobs(u, e, q);
        Guid firstInSql; using (var c = NewCtx()) firstInSql = await c.Set<RecalculationJob>().Where(j => j.UserId == u).OrderBy(j => j.FromDate).ThenBy(j => j.Id).Select(j => j.Id).FirstAsync();
        if (firstInSql != e.Id) queueProblems.Add("(vi) setup: E is not first in SQL order, scenario does not test target selection");
        string? error = null;
        try { await Enqueue(u, ciToday.AddDays(-13), ciToday.AddDays(-10), RecalculationCause.FoodLogChanged); } catch (Exception ex) { error = ex.GetType().Name; }
        var js = await JobsOf(u);
        var e2 = js.Single(j => j.Id == e.Id); var q2 = js.Single(j => j.Id == q.Id);
        if (!(error is null && js.Count == 2 && js.All(j => j.State == JobState.Pending)
              && q2.FromDate == a && q2.ToDate == ciToday.AddDays(-10) && q2.Cause == RecalculationCause.FoodLogChanged
              && e2.FromDate == a && e2.ToDate == ciToday.AddDays(-16) && e2.Cause == RecalculationCause.Manual))
            queueProblems.Add($"(vi) two pending jobs: error {error ?? "-"}, jobs [{string.Join(", ", js.Select(j => $"{(j.Id == e.Id ? "E" : "Q")} {j.State}/{j.Cause} {j.FromDate:MM-dd}..{j.ToDate:MM-dd}"))}]");
    }
    // Worker THẬT (ProcessAsync qua reflection, DbContext + UnitOfWork từ DI scope như prod) chạy job của u trong lúc phiên khác giữ khoá user ⇒ chờ 1.5 s rồi Busy ⇒ ReleaseAsync.
    // whileWaiting chạy khi đã thấy worker đang CHỜ khoá
    async Task<(bool Waiting, bool Done)> RunWorkerBusy(Guid u, Guid job, Func<ApplicationDbContext> workerCtx, Func<Task> whileWaiting)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => workerCtx());
        services.AddScoped<IUnitOfWork>(p => new UnitOfWork(p.GetRequiredService<ApplicationDbContext>()));
        using var provider = services.BuildServiceProvider();
        var worker = new DerivedDataWorker(provider.GetRequiredService<IServiceScopeFactory>(), Microsoft.Extensions.Options.Options.Create(new AlgorithmOptions { DerivedWorkerLockWaitMs = 1500 }),
            TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<DerivedDataWorker>.Instance);
        var process = typeof(DerivedDataWorker).GetMethod("ProcessAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        using var holderCtx = NewCtx();
        var holder = new UnitOfWork(holderCtx);
        await holder.BeginTransactionAsync();
        if (!await holder.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(u), 1000)) throw new InvalidOperationException("holder could not lock");
        var run = (Task<bool>)process.Invoke(worker, [job, u, CancellationToken.None])!;
        var waiting = await WaitForLockWaiter(run);
        await whileWaiting();
        var done = await run;   // hết 1.5 s chờ khoá ⇒ Busy ⇒ trả về
        await holder.RollbackTransactionAsync();
        return (waiting, done);
    }
    // Lỗi tạm thời ĐÚNG DẠNG prod: execution strategy mặc định của EF SQL Server bọc SqlException tạm thời (1205, 10054…) trong InvalidOperationException
    InvalidOperationException Transient() => new("An exception has been raised that is likely due to a transient failure.", new TransientDbException());

    // (vii) Rà soát vòng 5 (lỗi có sẵn ở worker): worker THẬT nhận J [day] (Running), khoá user bận; trong lúc chờ, một lần xếp job tạo job Pending D ĐÚNG khoảng [day]
    //       (EnqueueAsync chỉ gộp vào job Pending). Hết chờ ⇒ Busy ⇒ trả J về Pending ⇒ vi phạm unique. Trước: J Failed "duplicate key" (mất một lần thử).
    //       Sau: D mang FoodLogChanged của J, J xong (đã thay thế), không lỗi; lượt sau làm mới aggregate
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-3); await SeedStaleFoodDay(u, day);
        var job = Guid.NewGuid();
        await SeedJobs(u, NewJob(JobState.Pending, RecalculationCause.FoodLogChanged, day, day, job));
        var (waiting, done) = await RunWorkerBusy(u, job, NewCtx, () => Enqueue(u, day, day, RecalculationCause.WeightChanged));
        var js = await JobsOf(u); var midSt = await StateOf(u);
        var j = js.Single(x => x.Id == job); var others = js.Where(x => x.Id != job).ToList();
        if (others.Count == 1 && others[0].State == JobState.Pending) await Run(u, others[0].Id);
        var agg = await AggregateOf(u, day); var fin = await StateOf(u);
        if (!(waiting && !done && j.State == JobState.Succeeded && others.Count == 1 && others[0].State == JobState.Pending && others[0].Cause == RecalculationCause.FoodLogChanged
              && others[0].FromDate == day && others[0].ToDate == day && midSt == "Updating" && agg == 2100 && fin == "Ready"))
            queueProblems.Add($"(vii) worker release collides: waiting {waiting}, done {done}, J {j.State} \"{j.LastError}\", others [{string.Join(", ", others.Select(x => $"{x.State}/{x.Cause} {x.FromDate:MM-dd}..{x.ToDate:MM-dd}"))}], between {midSt}, final agg {agg} {fin}");
    }

    // (viii) Rà soát vòng 5 (thiếu phủ nhánh thử lại): hai lần xếp job cùng thấy "không có job Pending" rồi cùng INSERT đúng một khoảng (B commit ngay trước INSERT của A)
    //        ⇒ A gặp unique ⇒ bỏ job chưa lưu, đọc lại, gộp vào job của B (không 500): một job, FoodLogChanged
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-4);
        var hook = new BeforeCommand(c => c.CommandText.Contains("INSERT") && c.CommandText.Contains("[RecalculationJobs]"), () => Enqueue(u, day, day, RecalculationCause.WeightChanged));
        string? error = null;
        try { await Enqueue(u, day, day, RecalculationCause.FoodLogChanged, Hooked(hook)); } catch (Exception ex) { error = $"{ex.GetType().Name}: {ex.Message}"; }
        var js = await JobsOf(u);
        if (!(hook.Fired && error is null && js.Count == 1 && js[0].State == JobState.Pending && js[0].Cause == RecalculationCause.FoodLogChanged && js[0].FromDate == day && js[0].ToDate == day))
            queueProblems.Add($"(viii) insert vs insert: hook {hook.Fired}, error {error ?? "-"}, jobs [{string.Join(", ", js.Select(x => $"{x.State}/{x.Cause} {x.FromDate:MM-dd}..{x.ToDate:MM-dd}"))}]");
    }
    // (ix) Rà soát vòng 5+6: câu ĐỌC job Pending gặp lỗi tạm thời dạng prod (InvalidOperationException bọc DbException) một lần, ngoài transaction ⇒ thử lại.
    //      Trước: đọc nằm ngoài try, và bộ lọc chỉ nhận DbException ⇒ 500 sau khi nguồn đã lưu, không job
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-4);
        var hook = new BeforeCommand(c => c.CommandText.Contains("FROM [RecalculationJobs]"), () => Task.FromException(Transient()));
        string? error = null;
        try { await Enqueue(u, day, day, RecalculationCause.FoodLogChanged, Hooked(hook)); } catch (Exception ex) { error = ex.GetType().Name; }
        var js = await JobsOf(u);
        if (!(hook.Fired && error is null && js.Count == 1 && js[0].State == JobState.Pending && js[0].Cause == RecalculationCause.FoodLogChanged))
            queueProblems.Add($"(ix) transient read error: hook {hook.Fired}, error {error ?? "-"}, jobs {js.Count}");
    }

    // (x) Rà soát vòng 6+7: lỗi DB TRONG transaction của caller (ResumeFrom của DerivedDataSync, Retry). Quyết định theo transaction CÒN SỐNG hay không, không theo kiểu lỗi:
    //     a) transaction còn sống, lỗi tạm thời dạng EF ⇒ thử lại TRONG transaction, commit được, 1 job;
    //     b) server đã kết thúc transaction (ROLLBACK thật trên chính kết nối đó, như KILL/lỗi nặng) + lỗi KHÔNG bọc ⇒ không thử lại (sẽ ghi autocommit ngoài transaction) ⇒ ném, 0 job;
    //     c) như b nhưng lỗi dạng EF (deadlock victim) ⇒ ném, 0 job
    var brokenFlags = new List<string>();   // IsTransactionBroken tự nó: false khi còn sống, true sau ROLLBACK của server (không để bộ lọc nuốt lỗi của chính thuộc tính)
    async Task<(bool Fired, string? Error, int Jobs)> EnqueueInTx(bool killTx, Exception error)
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-4);
        var hook = new BeforeCommand(c => c.CommandText.Contains("FROM [RecalculationJobs]"), async c =>
        {
            if (killTx) { await using var k = c.Connection!.CreateCommand(); k.Transaction = c.Transaction; k.CommandText = "ROLLBACK"; await k.ExecuteNonQueryAsync(); }
            throw error;
        });
        string? caught = null;
        using (var c = Hooked(hook))
        {
            var tuow = new UnitOfWork(c);
            await tuow.BeginTransactionAsync();
            try { await DerivedDataJobs.EnqueueAsync(tuow, u, day, day, RecalculationCause.FoodLogChanged, null, default); await tuow.CommitTransactionAsync(); }
            catch (Exception ex)
            {
                caught = ex.GetType().Name;
                if (tuow.IsTransactionBroken != killTx) brokenFlags.Add($"killTx={killTx} but IsTransactionBroken={tuow.IsTransactionBroken}");
                try { await tuow.RollbackTransactionAsync(); } catch { /* transaction đã bị server kết thúc */ }
                if (tuow.IsTransactionBroken) brokenFlags.Add("IsTransactionBroken still true after RollbackTransactionAsync");
            }
        }
        return (hook.Fired, caught, (await JobsOf(u)).Count);
    }
    {
        var a = await EnqueueInTx(false, Transient());
        var b = await EnqueueInTx(true, new TransientDbException());
        var c = await EnqueueInTx(true, Transient());
        if (!(a is (true, null, 1) && b is (true, nameof(TransientDbException), 0) && c is (true, nameof(InvalidOperationException), 0)) || brokenFlags.Count > 0)
            queueProblems.Add($"(x) errors inside caller transaction: alive+wrapped {a}, dead+raw {b}, dead+wrapped {c} (expected (True, , 1) / (True, TransientDbException, 0) / (True, InvalidOperationException, 0)); flags [{string.Join("; ", brokenFlags)}]");
    }

    // (xi) Rà soát vòng 6: worker THẬT Busy, câu trả về Pending gặp lỗi tạm thời dạng prod một lần ⇒ thử lại, job về Pending, không tốn lần thử.
    //      Trước: ReleaseAsync chỉ bắt DbException ⇒ lỗi rơi vào nhánh lỗi của ProcessAsync ⇒ job chưa hề chạy bị đánh dấu Failed
    {
        var u = Guid.NewGuid(); var day = ciToday.AddDays(-3);
        var job = Guid.NewGuid();
        await SeedJobs(u, NewJob(JobState.Pending, RecalculationCause.WeightChanged, day, day, job));
        var hook = new BeforeCommand(c => c.CommandText.Contains("[AttemptCount] - 1"), () => Task.FromException(Transient()));
        var (waiting, done) = await RunWorkerBusy(u, job, () => Hooked(hook), () => Task.CompletedTask);
        var j = (await JobsOf(u)).Single();
        if (!(hook.Fired && waiting && !done && j.State == JobState.Pending && j.AttemptCount == 0 && j.LastError is null))
            queueProblems.Add($"(xi) transient error on release: hook {hook.Fired}, waiting {waiting}, done {done}, job {j.State} attempts {j.AttemptCount} \"{j.LastError}\"");
    }
    Check("9l hàng đợi job (11 thứ tự đua/lỗi ép tất định): đối chứng dương, gộp chỉ đổi ModifiedAt, worker xong giữa đọc/ghi, hai lần gộp đồng thời, giành lại quá lease, hai job Pending, worker trả về đụng unique, hai INSERT cùng khoảng, lỗi tạm khi đọc, lỗi tạm trong transaction, lỗi tạm khi trả về",
        queueProblems.Count == 0, queueProblems.Count == 0 ? "11/11 ok" : string.Join(" || ", queueProblems));

    // 9d) Ngân sách dòng: mọi ngày mang snapshot phiên bản mới hơn ("7") + log đều đổi ⇒ lần dựng đồng bộ của build cũ ("6", ngân sách 5 dòng) dừng ở 5 dòng daily và trả ResumeFrom,
    //     KHÔNG ghi hết cả chuỗi trong request; phần còn lại (như job bền) làm nốt; snapshot expenditure nhãn 7 không bị ghi đè
    var uBud = Guid.NewGuid();
    var budStart = ciToday.AddDays(-30);
    using (var c = NewCtx())
    {
        var prog = new MacroProgram { Id = Guid.NewGuid(), UserId = uBud, ProgramType = ProgramType.Coached, GoalType = GoalType.WeightLoss, StartDate = budStart, ExpenditureStartDate = budStart, ManualInitialTdeeKcal = 2500, CheckInWeekday = DayOfWeek.Monday, WeeklyWeightChangeTargetPercent = 0.5m, WeeklyWeightChangeTargetKg = -0.4m, ProteinGPerKgBodyweight = 2, IsActive = true };
        prog.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = budStart, CaloriesTarget = 2000, ProteinTarget = 150, CarbsTarget = 200, FatTarget = 60, AdjustmentReason = AdjustmentReason.Initial });
        c.Add(prog);
        for (var i = 0; i <= 30; i++)
        {
            var d = budStart.AddDays(i); var logId = Guid.NewGuid();
            c.Add(new FoodLog { Id = logId, UserId = uBud, LogDate = d, TotalCalories = 2000 });
            c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = 2000 });
            if (i % 2 == 0) c.Add(new WeightEntry { Id = Guid.NewGuid(), UserId = uBud, LogDate = d, WeightKg = 80m - i * 0.03m, TrendWeightKg = 80m - i * 0.03m });
        }
        await c.SaveChangesAsync();
    }
    using (var c = NewCtx()) { await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), uBud, budStart, ciToday, newer, default, null, ciToday); await c.SaveChangesAsync(); }
    async Task<string> ExpOf(Guid u)
    {
        using var c = NewCtx();
        return string.Join("|", (await c.Set<ExpenditureSnapshot>().Where(s => s.UserId == u).OrderBy(s => s.SnapshotDate).ToListAsync()).Select(s => $"{s.SnapshotDate:yyyyMMdd}:{s.EstimatedTdeeKcal}:{s.Status}:{s.AlgorithmVersion}"));
    }
    var expBefore = await ExpOf(uBud);
    using (var c = NewCtx()) await c.Set<FoodLog>().Where(l => l.UserId == uBud).ExecuteUpdateAsync(s => s.SetProperty(l => l.TotalCalories, l => l.TotalCalories + 100));
    ExpenditureRebuildResult bounded;
    using (var c = NewCtx())
    {
        bounded = await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), uBud, budStart, ciToday, older, default, new ExpenditureRebuildLimits(ciToday.AddDays(older.ExpenditureWindowDays), 5), ciToday);
        await c.SaveChangesAsync();
    }
    int freshAfterBounded;
    using (var c = NewCtx()) freshAfterBounded = await c.Set<DailyProgressSnapshot>().CountAsync(s => s.UserId == uBud && s.CaloriesConsumed == 2100);
    using (var c = NewCtx()) { await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), uBud, bounded.ResumeFrom ?? ciToday, ciToday, older, default, null, ciToday); await c.SaveChangesAsync(); }   // như job bền làm nốt
    int freshAfterJob;
    using (var c = NewCtx()) freshAfterJob = await c.Set<DailyProgressSnapshot>().CountAsync(s => s.UserId == uBud && s.CaloriesConsumed == 2100);
    var expAfter = await ExpOf(uBud);
    Check("9d ngân sách dòng trên ngày phiên bản mới hơn: dừng ở 5 dòng + ResumeFrom; job làm nốt 31 ngày; expenditure nhãn 7 giữ nguyên",
        bounded.RowsWritten == 5 && bounded.ResumeFrom == budStart.AddDays(5) && freshAfterBounded == 5 && freshAfterJob == 31 && expAfter == expBefore && expBefore.Contains(":7"),
        $"bounded rows {bounded.RowsWritten}, resume {bounded.ResumeFrom?.ToString("yyyy-MM-dd") ?? "null"} (expected {budStart.AddDays(5):yyyy-MM-dd}), fresh daily {freshAfterBounded} → {freshAfterJob}/31, expenditure unchanged {expAfter == expBefore}");

    // 9f) Rà soát vòng 1 (lỗi có sẵn): sửa một lần cân lẻ (không lần cân nào 20 ngày trước nó), lần cân kế tiếp cách 50 ngày ⇒ trend ở lần cân kế tiếp (và sau đó) đổi theo EMA.
    //     Phạm vi cũ (editedTo = ngày sửa) cho lần dựng đồng bộ "hội tụ" trước lần cân kế tiếp ⇒ snapshot từ đó trở đi cũ mà trạng thái vẫn Ready.
    //     Phạm vi mới (editedTo = lần cân muộn nhất có trend đổi) ⇒ sau lần ghi (và job nếu có) dựng lại toàn bộ từ đầu KHÔNG còn dòng nào để đổi.
    // Đuôi dài (250 ngày, cân mỗi 3 ngày tới hôm nay) để lần dựng theo phạm vi mới THỰC SỰ hội tụ trước hôm nay (rà soát vòng 2: đuôi 120 ngày không bao giờ chạm nhánh hội tụ)
    var gapStart = ciToday.AddDays(-250);
    var gapEdit = gapStart.AddDays(10);
    async Task<Guid> SeedWeightGapUser()
    {
        var u = Guid.NewGuid();
        using var c = NewCtx();
        var prog = new MacroProgram { Id = Guid.NewGuid(), UserId = u, ProgramType = ProgramType.Coached, GoalType = GoalType.WeightLoss, StartDate = gapStart, ExpenditureStartDate = gapStart, ManualInitialTdeeKcal = 2500, CheckInWeekday = DayOfWeek.Monday, WeeklyWeightChangeTargetPercent = 0.5m, WeeklyWeightChangeTargetKg = -0.4m, ProteinGPerKgBodyweight = 2, IsActive = true };
        prog.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = gapStart, CaloriesTarget = 2000, ProteinTarget = 150, CarbsTarget = 200, FatTarget = 60, AdjustmentReason = AdjustmentReason.Initial });
        c.Add(prog);
        for (var i = 0; i <= 250; i++)
        {
            var d = gapStart.AddDays(i); var logId = Guid.NewGuid();
            c.Add(new FoodLog { Id = logId, UserId = u, LogDate = d, TotalCalories = 2000 });
            c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = 2000 });
        }
        c.Add(new WeightEntry { Id = Guid.NewGuid(), UserId = u, LogDate = gapEdit, WeightKg = 80m });
        for (var i = 60; i <= 250; i += 3) c.Add(new WeightEntry { Id = Guid.NewGuid(), UserId = u, LogDate = gapStart.AddDays(i), WeightKg = 79m - (i - 60) * 0.01m });
        await c.SaveChangesAsync();
        using var t = NewCtx();
        var all = await t.Set<WeightEntry>().Where(w => w.UserId == u).ToListAsync();
        WeightTrendRefresher.Apply(all, older.TrendDays);
        await t.SaveChangesAsync();
        await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(t), u, gapStart, ciToday, older, default, null, ciToday);
        await t.SaveChangesAsync();
        return u;
    }
    async Task<int> RowsAFullRebuildWouldWrite(Guid u)
    {
        using var c = NewCtx();
        return (await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), u, gapStart, ciToday, older, default, null, ciToday)).RowsWritten;   // không lưu: chỉ đo
    }
    async Task FinishJobs(Guid u)
    {
        Guid? job; using (var c = NewCtx()) job = await c.Set<RecalculationJob>().Where(j => j.UserId == u && j.State == JobState.Pending).Select(j => (Guid?)j.Id).FirstOrDefaultAsync();
        if (job is { } id) using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), u, id, older, TimeProvider.System, default);
    }

    var uGapOld = await SeedWeightGapUser();
    using (var c = NewCtx())   // phạm vi CŨ: cùng thay đổi nguồn, rồi dựng với editedTo = ngày sửa (đúng lời gọi trước khi sửa)
    {
        var all = await c.Set<WeightEntry>().Where(w => w.UserId == uGapOld).ToListAsync();
        all.Single(w => w.LogDate == gapEdit).WeightKg = 82m;
        WeightTrendRefresher.Apply(all, older.TrendDays);
        await c.SaveChangesAsync();
        await DerivedDataSync.RunAsync(new UnitOfWork(c), uGapOld, gapEdit, gapEdit, null, RecalculationCause.WeightChanged, older, TimeProvider.System, default);
    }
    await FinishJobs(uGapOld);
    var staleOld = await RowsAFullRebuildWouldWrite(uGapOld);

    var uGapNew = await SeedWeightGapUser();
    DateOnly? changedTo;
    using (var c = NewCtx())
    {
        var probe = await c.Set<WeightEntry>().Where(w => w.UserId == uGapNew).AsNoTracking().ToListAsync();
        probe.Single(w => w.LogDate == gapEdit).WeightKg = 82m;
        changedTo = WeightTrendRefresher.Apply(probe, older.TrendDays);   // chỉ để báo cáo, không lưu
    }
    using (var c = NewCtx()) await new UpsertWeightEntryCommandHandler(new UnitOfWork(c), new TestUser(uGapNew), Microsoft.Extensions.Options.Options.Create(older), TimeProvider.System).Handle(new UpsertWeightEntryCommand(gapEdit, 82m, null), default);
    await FinishJobs(uGapNew);
    var staleNew = await RowsAFullRebuildWouldWrite(uGapNew);
    Check("9f sửa lần cân lẻ trước khoảng trống 50 ngày: phạm vi cũ để snapshot cũ (đối chứng); handler đã sửa để KHÔNG còn dòng nào cũ",
        staleOld > 0 && staleNew == 0 && changedTo > gapEdit.AddDays(41),
        $"old scope leaves {staleOld} stale rows; fixed handler leaves {staleNew}; trend changed up to {changedTo:yyyy-MM-dd} (edit {gapEdit:yyyy-MM-dd}, next weigh-in {gapStart.AddDays(60):yyyy-MM-dd})");

    // 9f') Cận mới có AN TOÀN khi thực sự dừng sớm: dựng có giới hạn với ConvergeAfter = (lần cân muộn nhất có trend đổi) + W phải HỘI TỤ trước hôm nay, rồi dựng lại toàn bộ không còn dòng nào để đổi
    var uGapBound = await SeedWeightGapUser();
    ExpenditureRebuildResult boundRun; DateOnly? boundTo;
    using (var c = NewCtx())
    {
        var all = await c.Set<WeightEntry>().Where(w => w.UserId == uGapBound).ToListAsync();
        all.Single(w => w.LogDate == gapEdit).WeightKg = 82m;
        boundTo = WeightTrendRefresher.Apply(all, older.TrendDays);
        await c.SaveChangesAsync();
        boundRun = await ExpenditureRebuilder.RebuildAsync(new UnitOfWork(c), uGapBound, gapEdit, ciToday, older, default, new ExpenditureRebuildLimits(boundTo!.Value.AddDays(older.ExpenditureWindowDays), int.MaxValue), ciToday);
        await c.SaveChangesAsync();
    }
    var staleBound = await RowsAFullRebuildWouldWrite(uGapBound);
    Check("9f' dựng với cận mới thực sự hội tụ sớm (trước hôm nay) và vẫn đúng: dựng lại toàn bộ sau đó không đổi dòng nào",
        boundRun.Converged && boundRun.ResumeFrom is null && staleBound == 0 && boundTo == changedTo,
        $"converged {boundRun.Converged} after {boundRun.RowsWritten} rows (ConvergeAfter {boundTo?.AddDays(older.ExpenditureWindowDays):yyyy-MM-dd}, today {ciToday:yyyy-MM-dd}); full rebuild would still write {staleBound}");

    // 9e) POST chỉ để tính: filter thật bỏ qua (không truy vấn, không header, action vẫn chạy); danh sách đánh dấu KHỚP READ_ONLY_POSTS của FE; không đường GHI nào khớp regex đó
    using (var c = CountedCtx())
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST"; http.Request.Path = "/api/goals/edit-preview";
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor { EndpointMetadata = [new SkipDerivedDataStateAttribute()] });
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        var filter = new DerivedDataStateFilter(new StatusSender(new UnitOfWork(c), me, ciOpt), Microsoft.Extensions.Logging.Abstractions.NullLogger<DerivedDataStateFilter>.Instance);
        var ran = false;
        sql.Reset();
        await filter.OnActionExecutionAsync(executing, () => { ran = true; return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()) { Result = new OkObjectResult(1) }); });
        Check("9e filter: action [SkipDerivedDataState] ⇒ action vẫn chạy, 0 truy vấn, không header", ran && sql.Count == 0 && !http.Response.Headers.ContainsKey(DerivedDataStateHeader.Name), $"ran {ran}, statements {sql.Count}, header {(http.Response.Headers.ContainsKey(DerivedDataStateHeader.Name) ? "present" : "absent")}");
    }

    // 9e') Rà soát vòng 1 (thiếu phủ): chứng minh MVC THẬT đưa attribute cấp action vào ActionDescriptor.EndpointMetadata (không dựng tay), rồi chạy filter trên descriptor thật đó
    {
        var mvc = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions.AddLogging(mvc);
        Microsoft.Extensions.DependencyInjection.MvcCoreMvcBuilderExtensions.AddApplicationPart(
            Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions.AddControllers(mvc), typeof(DerivedDataStateFilter).Assembly);
        using var sp = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(mvc);
        var descriptors = ((Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider)sp.GetService(typeof(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider))!)
            .ActionDescriptors.Items.OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>().ToList();
        bool Marked(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor d) => d.EndpointMetadata.OfType<SkipDerivedDataStateAttribute>().Any();
        var markedReal = descriptors.Where(Marked).Select(d => $"{d.ControllerName}.{d.ActionName}").OrderBy(x => x).ToList();
        var expectedReal = new[] { "FoodLogAi.ResolveIngredient", "FoodLogs.GetPlateNutrients", "Goals.PreviewEdit", "Onboarding.Preview", "Programs.Preview" };
        var weightsUpsert = descriptors.Single(d => d.ControllerName == "Weights" && d.ActionName == "Upsert");
        var goalsPreview = descriptors.Single(d => d.ControllerName == "Goals" && d.ActionName == "PreviewEdit");

        using var c = CountedCtx();
        async Task<(bool Ran, int Statements, bool Header)> RunFilterOn(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor d, string path)
        {
            var http = new DefaultHttpContext();
            http.Request.Method = "POST"; http.Request.Path = path;
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
            var ac = new ActionContext(http, new RouteData(), d);
            var exec = new ActionExecutingContext(ac, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
            var f = new DerivedDataStateFilter(new StatusSender(new UnitOfWork(c), me, ciOpt), Microsoft.Extensions.Logging.Abstractions.NullLogger<DerivedDataStateFilter>.Instance);
            var ran = false; sql.Reset();
            await f.OnActionExecutionAsync(exec, () => { ran = true; return Task.FromResult(new ActionExecutedContext(ac, new List<IFilterMetadata>(), new object()) { Result = new OkObjectResult(1) }); });
            return (ran, sql.Count, http.Response.Headers.ContainsKey(DerivedDataStateHeader.Name));
        }
        var onPreview = await RunFilterOn(goalsPreview, "/api/goals/edit-preview");
        var onWrite = await RunFilterOn(weightsUpsert, "/api/weights");
        Check("9e' MVC thật: EndpointMetadata của đúng 5 action có [SkipDerivedDataState]; filter trên descriptor thật bỏ qua preview, vẫn gắn header cho POST /weights",
            markedReal.SequenceEqual(expectedReal) && !Marked(weightsUpsert) && onPreview is { Ran: true, Statements: 0, Header: false } && onWrite is { Ran: true, Statements: 1, Header: true },
            $"marked [{string.Join(", ", markedReal)}] of {descriptors.Count} actions; preview {onPreview}; weights {onWrite}");

        // 9e parity (rà soát vòng 2): đối chiếu FE ⇄ BE trên route + verb THẬT của MVC (không dựng tay). READ_ONLY_POSTS đọc từ FE/src/lib/axios.ts (tìm ngược thư mục cha);
        // không tìm thấy file / không đọc được khai báo ⇒ FAIL (không còn rơi về bản sao trong file test)
        string? feFile = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null && feFile is null; dir = dir.Parent)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "FE", "src", "lib", "axios.ts");
            if (File.Exists(candidate)) feFile = candidate;
        }
        var feMatch = feFile is null ? null : System.Text.RegularExpressions.Regex.Match(File.ReadAllText(feFile), @"READ_ONLY_POSTS\b[^=\n]*=\s*/(.+)/[a-z]*\s*;?\s*(?://.*)?$", System.Text.RegularExpressions.RegexOptions.Multiline);
        var feRegex = feMatch is { Success: true } ? feMatch.Groups[1].Value : null;
        var readOnly = feRegex is null ? null : new System.Text.RegularExpressions.Regex(feRegex);
        string FeUrl(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor d)
        {
            var full = "/" + (d.AttributeRouteInfo?.Template ?? "").Trim('/');
            return full.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ? full[4..] : full;   // FE gọi tương đối với baseURL .../api
        }
        List<string> Verbs(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor d)
            => (d.ActionConstraints ?? []).OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().SelectMany(x => x.HttpMethods).ToList();
        var markedUrls = descriptors.Where(Marked).Select(FeUrl).OrderBy(x => x).ToList();
        var expectedUrls = new[] { "/food-logs/ai/resolve-ingredient", "/food-logs/plate/nutrients", "/goals/edit-preview", "/onboarding/preview", "/programs/preview" };
        var markedOk = readOnly is not null && descriptors.Where(Marked).All(d => Verbs(d).SequenceEqual(["POST"]) && readOnly.IsMatch(FeUrl(d)));
        var writesFeIgnores = readOnly is null ? new List<string>()
            : descriptors.Where(d => !Marked(d) && Verbs(d).Any(v => v is "POST" or "PUT" or "PATCH" or "DELETE") && readOnly.IsMatch(FeUrl(d))).Select(d => $"{string.Join("/", Verbs(d))} {FeUrl(d)}").ToList();
        Check("9e parity (route MVC thật): 5 POST chỉ-tính = đúng các đường FE bỏ qua (READ_ONLY_POSTS đọc từ FE/src/lib/axios.ts); không đường ghi nào FE bỏ qua header",
            feRegex is not null && markedUrls.SequenceEqual(expectedUrls) && markedOk && writesFeIgnores.Count == 0,
            $"FE file {feFile ?? "NOT FOUND"}; regex {feRegex ?? "NOT READ"}; marked [{string.Join(", ", markedUrls)}]; writes FE would ignore [{string.Join(", ", writesFeIgnores)}]; {descriptors.Count} actions");
    }
}
finally { await Drop(); Console.WriteLine($"dropped temporary database {db}"); }

Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAIL");
return failed == 0 ? 0 : 1;

// Hook tất định cho 9g: chạy một hành động (commit quyết định đối thủ) đúng MỘT lần, ngay trước câu SQL khớp điều kiện / ngay trước SaveChanges của context bên thua
sealed class BeforeReader(Func<System.Data.Common.DbCommand, bool> when, Func<Task> action) : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    public bool Fired;
    public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> r, CancellationToken ct = default)
    {
        if (!Fired && when(c)) { Fired = true; await action(); }
        return r;
    }
}

sealed class TransientDbException() : System.Data.Common.DbException("simulated transient error");

sealed class BeforeCommand(Func<System.Data.Common.DbCommand, bool> when, Func<System.Data.Common.DbCommand, Task> action) : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    public BeforeCommand(Func<System.Data.Common.DbCommand, bool> when, Func<Task> action) : this(when, _ => action()) { }
    public bool Fired;
    public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> NonQueryExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> r, CancellationToken ct = default)
    {
        if (!Fired && when(c)) { Fired = true; await action(c); }
        return r;
    }
    // INSERT của SaveChanges có thể chạy dạng reader
    public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> r, CancellationToken ct = default)
    {
        if (!Fired && when(c)) { Fired = true; await action(c); }
        return r;
    }
}

sealed class BeforeSave(Func<Task> action) : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    public bool Fired;
    public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> r, CancellationToken ct = default)
    {
        if (!Fired) { Fired = true; await action(); }
        return r;
    }
}

sealed class SqlCounter : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    public int Count;
    public List<string> Texts = [];
    public void Reset() { Count = 0; Texts.Clear(); }
    private void Add(System.Data.Common.DbCommand c) { Count++; Texts.Add(c.CommandText); }
    public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> r, CancellationToken ct = default) { Add(c); return new(r); }
    public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> NonQueryExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> r, CancellationToken ct = default) { Add(c); return new(r); }
}

sealed record TestUser(Guid UserId) : IUserContext
{
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}

// ISender tối thiểu cho filter: chỉ trả trạng thái dựng lại THẬT từ DB (cùng hàm GET /derived-data/status dùng), không qua pipeline MediatR
sealed class StatusSender(IUnitOfWork uow, IUserContext user, AlgorithmOptions opt) : ISender
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => request switch
        {
            GetDerivedDataStateQuery => (TResponse)(object)await DerivedDataJobs.OpenStateAsync(uow, user.UserId, opt, cancellationToken),
            GetDerivedDataStatusQuery => (TResponse)(object)await DerivedDataJobs.StatusAsync(uow, user.UserId, opt, cancellationToken),
            _ => throw new NotSupportedException(),
        };
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
    public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
