// NDM ↔ Food Log (P0 + S29 + round 2): handler THẬT trên LocalDB tạm (migrate schema thật), mỗi request một DbContext + UnitOfWork như prod. Không API, không DB dùng chung.
// Phần 0: migration S29 Up/Down + backup/restore. Phần 1+: Manual Nutrition (override thắng Food Log), null vs 0, nhịn ăn, tự bỏ cờ chưa-đủ (R2-4), dẫn xuất, hợp đồng v2, đồng thời, lỗi giao dịch.
using Application;
using Contract;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_NdmRepro";
const string s28 = "20261023103247_S28_GoalTrackingMode";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";   // MARS như prod
// AuditSaveChangesInterceptor như prod: Delete của ISoftDeletable (FoodLogEntry) thành xoá mềm; thiếu nó thì Remove xoá cứng và test sẽ không thấy "xoá mềm"
var audit = new AuditSaveChangesInterceptor(new Microsoft.AspNetCore.Http.HttpContextAccessor(), TimeProvider.System);
DbContextOptions<ApplicationDbContext> Opts(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] extra)
    => new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors([audit, .. extra]).Options;
var opts = Opts();
ApplicationDbContext NewCtx() => new(opts);
ApplicationDbContext LegacyCtx() => new LegacyContext(opts);   // mô hình KHÔNG có 4 cột manual: dựng dữ liệu cũ trên schema S28
var algo = Options.Create(new AlgorithmOptions());

async Task Drop()
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand();
    cmd.CommandText = $"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END";
    await cmd.ExecuteNonQueryAsync();
}
async Task<T?> Scalar<T>(string sql)
{
    await using var c = new Microsoft.Data.SqlClient.SqlConnection(cs);
    await c.OpenAsync();
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    var v = await cmd.ExecuteScalarAsync();
    return v is null or DBNull ? default : (T)Convert.ChangeType(v, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
}
async Task Exec(string sql)
{
    await using var c = new Microsoft.Data.SqlClient.SqlConnection(cs);
    await c.OpenAsync();
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

var failed = 0;
void Check(string name, bool ok, string detail) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name} — {detail}"); if (!ok) failed++; }
var today = DateOnly.FromDateTime(DateTime.UtcNow);

// ---- GlobalExceptionHandler THẬT (internal ⇒ dựng bằng reflection): mọi lỗi của handler đi qua đúng bộ ánh xạ mà API dùng, không tự dịch trong test ----
var handlerType = typeof(API.NutritionDataController).Assembly.GetType("API.GlobalExceptionHandler")!;
var exHandler = (IExceptionHandler)Activator.CreateInstance(handlerType, typeof(NullLogger<>).MakeGenericType(handlerType).GetField("Instance")!.GetValue(null), new TestEnv())!;
async Task<(int Status, string? Reason, string Body)> Map(Exception e)
{
    var ctx = new DefaultHttpContext(); ctx.Response.Body = new MemoryStream();
    await exHandler.TryHandleAsync(ctx, e, default);
    ctx.Response.Body.Position = 0;
    var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
    using var doc = JsonDocument.Parse(body);
    string? reason = null;
    if (doc.RootElement.TryGetProperty("errors", out var er) && er.ValueKind == JsonValueKind.Object && er.TryGetProperty("reasonCode", out var rc)) reason = rc[0].GetString();
    else if (doc.RootElement.TryGetProperty("message", out var m)) reason = m.GetString();
    return (ctx.Response.StatusCode, reason, body);
}
// ---- gọi handler thật; trả (status, reasonCode, giá trị) ----
async Task<(int Status, string? Reason, T? Value)> Call<T>(Func<UnitOfWork, Task<T>> f, Func<ApplicationDbContext>? make = null)
{
    using var c = (make ?? NewCtx)();
    try { return (200, null, await f(new UnitOfWork(c))); }
    catch (Exception e) { var (status, reason, _) = await Map(e); return (status, reason, default); }
}
IUserClock Clock(UnitOfWork uow, Guid u) => new UserClock(uow, new TestUser(u), TimeProvider.System);
Task<(int Status, string? Reason, NutritionDataDayModel? Value)> Save(Guid u, DateOnly d, decimal? kcal = null, decimal? p = null, decimal? f = null, decimal? c = null, bool? fasting = null, bool? incomplete = null, Func<ApplicationDbContext>? make = null, IOptions<AlgorithmOptions>? opt = null)
    => Call(uow => new SaveNutritionDataCommandHandler(uow, new TestUser(u), TimeProvider.System, opt ?? algo).Handle(new SaveNutritionDataCommand(d, kcal, p, f, c, fasting, incomplete), default), make);
Task<(int Status, string? Reason, Guid Value)> QuickEdit(Guid u, DateOnly d, decimal kcal, decimal p = 0, decimal c = 0, decimal f = 0, Func<ApplicationDbContext>? make = null)
    => Call(uow => new QuickEditDayCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new QuickEditDayCommand(d, null, kcal, p, c, f, null, null), default), make);
Task<(int Status, string? Reason, MediatR.Unit Value)> Clear(Guid u, DateOnly d)
    => Call(uow => new ClearDayLogCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new ClearDayLogCommand(d), default));
Task<(int Status, string? Reason, MediatR.Unit Value)> DeleteNdm(Guid u, DateOnly d)
    => Call(uow => new DeleteNutritionDataCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new DeleteNutritionDataCommand(d), default));
Task<(int Status, string? Reason, NutritionDataDayModel? Value)> RemoveOverride(Guid u, DateOnly d)
    => Call(uow => new RemoveManualOverrideCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new RemoveManualOverrideCommand(d), default));
Task<(int Status, string? Reason, NutritionDataDayModel? Value)> SetFasting(Guid u, DateOnly d, bool on)
    => Call(uow => new SetDayFastingCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new SetDayFastingCommand(d, on), default));
Task<(int Status, string? Reason, NutritionDataDayModel? Value)> SetIncomplete(Guid u, DateOnly d, bool on)
    => Call(uow => new SetDayIncompleteCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new SetDayIncompleteCommand(d, on), default));
Task<(int Status, string? Reason, Guid Value)> AddFood(Guid u, DateOnly d, decimal kcal, decimal p = 10, decimal c = 20, decimal f = 5)
    => Call(uow => new QuickAddCommandHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo).Handle(new QuickAddCommand(d, null, null, "food", kcal, p, c, f), default));
Task<(int Status, string? Reason, MediatR.Unit Value)> DeleteFood(Guid u, Guid entryId)
    => Call(uow => new DeleteFoodLogEntryCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new DeleteFoodLogEntryCommand(entryId), default));
Task<(int Status, string? Reason, int Value)> BulkDelete(Guid u, params Guid[] ids)
    => Call(uow => new BulkDeleteEntriesCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new BulkDeleteEntriesCommand(ids.ToList()), default));
Task<(int Status, string? Reason, MediatR.Unit Value)> EditFoodGrams(Guid u, Guid entryId, decimal grams)
    => Call(uow => new UpdateFoodLogEntryCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new UpdateFoodLogEntryCommand(entryId, null, null, null, grams), default));
Task<(int Status, string? Reason, NutritionDataResponse? Value)> NdmList(Guid u, DateOnly? from = null, DateOnly? to = null, int? limit = null, DateOnly? before = null)
    => Call(uow => new GetNutritionDataQueryHandler(uow, new TestUser(u)).Handle(new GetNutritionDataQuery(from, to, limit, before), default));
async Task<NutritionDataDayModel?> Day(Guid u, DateOnly d) => (await NdmList(u, d, d)).Value?.Items.FirstOrDefault();
async Task<Dictionary<DateOnly, FoodLogSupport.LoggedDay>> Days(Guid u, DateOnly from, DateOnly to, bool fasting = true)
{ using var c = NewCtx(); return await FoodLogSupport.LoadDaysAsync(new UnitOfWork(c), u, from, to, default, fasting); }

// ---- seed: ngày với món (Source Search), entry Quick Add cũ (Source QuickAdd), manual (4 cột), nước, ghi chú, cờ ----
async Task<Guid> Seed(Guid u, DateOnly d, int foods = 0, decimal kcalEach = 600, decimal? legacyQuickAdd = null, decimal? mk = null, decimal? mp = null, decimal? mc = null, decimal? mf = null,
    int waterMl = 0, bool note = true, bool incomplete = false, bool fasting = false, Func<ApplicationDbContext>? make = null)
{
    using var c = (make ?? NewCtx)();
    var logId = Guid.NewGuid();
    var total = foods * kcalEach + (legacyQuickAdd ?? 0);
    var log = new FoodLog { Id = logId, UserId = u, LogDate = d, TotalCalories = total, TotalProtein = foods * 10, TotalCarbs = foods * 20, TotalFat = foods * 5, IsIncomplete = incomplete, IsFasting = fasting, Notes = "day note" };
    if (make is null) (log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG) = (mk, mp, mc, mf);
    c.Add(log);
    for (var i = 0; i < foods; i++)
        c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = kcalEach, Protein = 10, Carbs = 20, Fat = 5, EatenAt = new TimeOnly(8 + i % 10, 0) });
    if (legacyQuickAdd is { } q)
        c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 100, Calories = q, Source = EntrySource.QuickAdd });
    if (waterMl > 0) c.Add(new WaterLog { Id = Guid.NewGuid(), UserId = u, FoodLogId = logId, AmountMl = waterMl, LoggedAt = DateTimeOffset.UtcNow });
    if (note) c.Add(new FoodLogNote { Id = Guid.NewGuid(), UserId = u, LogDate = d, Text = "a note" });
    await c.SaveChangesAsync();
    return logId;
}
async Task<List<Guid>> LiveEntryIds(Guid u, DateOnly d)
{ using var c = NewCtx(); return await c.Set<FoodLogEntry>().Where(e => e.FoodLog.UserId == u && e.FoodLog.LogDate == d).OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(); }

// ---- trạng thái của một ngày (kể cả entry đã xoá mềm) ----
async Task<Snap> Snapshot(Guid u, DateOnly d)
{
    using var c = NewCtx();
    var log = await c.Set<FoodLog>().AsNoTracking().FirstOrDefaultAsync(l => l.UserId == u && l.LogDate == d);
    if (log is null) return new Snap(false, null, 0, 0, 0, 0, 0, false, false, null, "", null, null, null, null);
    var all = await c.Set<FoodLogEntry>().IgnoreQueryFilters().AsNoTracking().Where(e => e.FoodLogId == log.Id).OrderBy(e => e.Id).ToListAsync();
    return new Snap(true, log.Id, all.Count(e => !e.IsDeleted), all.Count(e => e.IsDeleted), log.TotalCalories,
        await c.Set<WaterLog>().CountAsync(w => w.FoodLogId == log.Id), await c.Set<FoodLogNote>().IgnoreQueryFilters().CountAsync(n => n.UserId == u && n.LogDate == d),
        log.IsFasting, log.IsIncomplete, log.Notes, string.Join(",", all.Select(e => $"{e.Id:N}:{e.IsDeleted}:{e.Calories}:{e.Source}")),
        log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG);
}
async Task<int> FoodItemCount(Guid u) { using var c = NewCtx(); return await c.Set<FoodItem>().IgnoreQueryFilters().CountAsync(f => f.CreatedByUserId == u); }
string Statuses<T>(IEnumerable<(int Status, string? Reason, T? Value)> rs) => string.Join("/", rs.Select(r => r.Status + (r.Reason is null ? "" : ":" + r.Reason)));
string StatusList(IEnumerable<(int Status, string? Reason)> rs) => string.Join("/", rs.Select(r => r.Status + (r.Reason is null ? "" : ":" + r.Reason)));
async Task<(int Status, string? Reason)> St<T>(Task<(int Status, string? Reason, T? Value)> t) { var r = await t; return (r.Status, r.Reason); }   // đưa các kết quả khác kiểu về cùng một kiểu để Task.WhenAll trả mảng
async Task<(decimal? Consumed, bool? Logged)> DailyProgress(Guid u, DateOnly d)
{ using var c = NewCtx(); var s = await c.Set<DailyProgressSnapshot>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == u && x.SnapshotDate == d); return (s?.CaloriesConsumed, s?.IsLogged); }
async Task SeedProgram(Guid u, DateOnly start) { using var c = NewCtx(); c.Add(new MacroProgram { Id = Guid.NewGuid(), UserId = u, StartDate = start, ExpenditureStartDate = start, ManualInitialTdeeKcal = 2500, IsActive = true }); await c.SaveChangesAsync(); }

await Drop();
try
{
    // =========================== 0) MIGRATION S29: Up / schema / dữ liệu cũ / ràng buộc / Down có sao lưu ===========================
    var legacyUser = Guid.NewGuid(); var ld1 = today.AddDays(-20); var ld2 = today.AddDays(-19);
    using (var c = NewCtx())
    {
        await c.GetService<IMigrator>().MigrateAsync(s28);   // schema TRƯỚC S29
        await c.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");   // dữ liệu thử, không cần user/food thật
    }
    var colsBefore = await Scalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
    await Seed(legacyUser, ld1, foods: 3, kcalEach: 600, legacyQuickAdd: 400, waterMl: 500, incomplete: true, make: LegacyCtx);   // ngày cũ: món + Quick Add cũ + nước + ghi chú + cờ chưa đủ
    await Seed(legacyUser, ld2, fasting: true, waterMl: 100, make: LegacyCtx);
    string Fp() => "SELECT CONCAT((SELECT COUNT(*) FROM FoodLogs),'|',(SELECT COUNT(*) FROM FoodLogEntries),'|',(SELECT COUNT(*) FROM WaterLogs),'|',(SELECT COUNT(*) FROM FoodLogNotes),'|',"
        + "(SELECT CHECKSUM_AGG(CHECKSUM(Id,TotalCalories,TotalProtein,TotalCarbs,TotalFat,IsFasting,IsIncomplete,Notes)) FROM FoodLogs),'|',(SELECT CHECKSUM_AGG(CHECKSUM(Id,FoodLogId,Calories,Source,IsDeleted)) FROM FoodLogEntries))";
    var fpBefore = (await Scalar<string>(Fp()))!;

    using (var c = NewCtx()) await c.Database.MigrateAsync();   // S29 Up
    await Exec("ALTER TABLE FoodLogs WITH CHECK CHECK CONSTRAINT CK_FoodLogs_ManualNutrition_Range");   // sp_MSforeachtable đã tắt cả CHECK; bật lại để thử ràng buộc thật
    var fpAfter = (await Scalar<string>(Fp()))!;
    var colInfo = await Scalar<string>("SELECT STRING_AGG(CONCAT(COLUMN_NAME,':',DATA_TYPE,':',IS_NULLABLE,':',NUMERIC_PRECISION,':',NUMERIC_SCALE,':',ISNULL(COLUMN_DEFAULT,'nodefault')), ',') WITHIN GROUP (ORDER BY COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
    var hasCk = await Scalar<int>("SELECT COUNT(*) FROM sys.check_constraints WHERE name = 'CK_FoodLogs_ManualNutrition_Range'");
    var lastMig = await Scalar<string>("SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC");
    Check("0a Migration Up S29: 4 cột decimal(8,2) NULL, không default, có CHECK; lịch sử migration kết thúc ở S29",
        colsBefore == 0 && colInfo == "ManualCaloriesKcal:decimal:YES:8:2:nodefault,ManualCarbsG:decimal:YES:8:2:nodefault,ManualFatG:decimal:YES:8:2:nodefault,ManualProteinG:decimal:YES:8:2:nodefault" && hasCk == 1 && lastMig!.EndsWith("S29_ManualNutrition"),
        $"cột trước {colsBefore}, sau [{colInfo}], CHECK {hasCk}, migration cuối {lastMig}");
    var manualNonNull = await Scalar<int>("SELECT COUNT(*) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
    var legacySnap = await Snapshot(legacyUser, ld1);
    Check("0b Existing Food Logs không đổi sau Up: số dòng + checksum FoodLogs/FoodLogEntries/nước/ghi chú giữ nguyên, 4 cột manual đều NULL, Quick Add cũ (Source=QuickAdd) vẫn là entry sống, không chuyển đổi",
        fpBefore == fpAfter && manualNonNull == 0 && legacySnap is { Live: 4, Deleted: 0, Water: 1, Notes: 1, Incomplete: true } && legacySnap.EntryIds.Contains(":QuickAdd"),
        $"before {fpBefore} / after {fpAfter}, manual non-null {manualNonNull}, legacy live {legacySnap.Live}, quickAdd kept {legacySnap.EntryIds.Contains(":QuickAdd")}");

    var anyLog = await Scalar<string>($"SELECT TOP 1 CONVERT(varchar(36), Id) FROM FoodLogs WHERE UserId = '{legacyUser}' AND LogDate = '{ld1:yyyy-MM-dd}'");
    async Task<string?> SqlErr(string set) { try { await Exec($"UPDATE FoodLogs SET {set} WHERE Id = '{anyLog}'"); return null; } catch (Microsoft.Data.SqlClient.SqlException e) { return e.Number.ToString(); } }
    var errs = new[] { await SqlErr("ManualCaloriesKcal = -1"), await SqlErr("ManualCaloriesKcal = 10000.01"), await SqlErr("ManualProteinG = 1000.01"), await SqlErr("ManualCarbsG = -0.01"), await SqlErr("ManualFatG = 1001") };
    var okSets = new[] { await SqlErr("ManualCaloriesKcal = 0, ManualProteinG = 0, ManualCarbsG = 0, ManualFatG = 0"), await SqlErr("ManualCaloriesKcal = 10000, ManualProteinG = 1000, ManualCarbsG = 1000, ManualFatG = 1000") };
    await SqlErr("ManualProteinG = 12.345, ManualCaloriesKcal = 2200.456");
    var rounded = await Scalar<string>($"SELECT CONCAT(ManualProteinG,'|',ManualCaloriesKcal) FROM FoodLogs WHERE Id = '{anyLog}'");
    Check("0c Ràng buộc/numeric: ngoài khoảng (âm, > 10000/1000) bị CHECK chặn (547), biên 0 và 10000/1000 hợp lệ, decimal(8,2) làm tròn 2 chữ số",
        errs.All(e => e == "547") && okSets.All(e => e is null) && rounded == "12.35|2200.46", $"lỗi {string.Join(",", errs.Select(e => e ?? "OK"))}, biên {string.Join(",", okSets.Select(e => e ?? "OK"))}, làm tròn {rounded}");
    await Exec($"UPDATE FoodLogs SET ManualCaloriesKcal = 2200, ManualProteinG = NULL, ManualCarbsG = 250, ManualFatG = 0 WHERE Id = '{anyLog}'");   // dữ liệu manual thật cho phép thử rollback

    // Down CÓ SAO LƯU: xuất 4 cột ra bảng sao lưu, rollback S29, kiểm dữ liệu cũ nguyên vẹn, Up lại, khôi phục từ sao lưu
    var manualBefore = await Scalar<string>("SELECT CONCAT(COUNT(*),'|',SUM(ManualCaloriesKcal),'|',SUM(ISNULL(ManualProteinG,-1)),'|',SUM(ManualCarbsG),'|',SUM(ManualFatG)) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
    await Exec("SELECT Id, UserId, LogDate, ManualCaloriesKcal, ManualProteinG, ManualCarbsG, ManualFatG INTO dbo.FoodLogManualBackup FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
    // Down BỊ TỪ CHỐI khi còn dữ liệu manual (không âm thầm Drop): DB ở nguyên S29, cột + dữ liệu còn nguyên
    string? refusal = null;
    try { using var rc = NewCtx(); await rc.GetService<IMigrator>().MigrateAsync(s28); } catch (Exception e) { refusal = (e.InnerException ?? e).Message; }
    var colsAfterRefusal = await Scalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
    var lastAfterRefusal = await Scalar<string>("SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC");
    var manualAfterRefusal = await Scalar<string>("SELECT CONCAT(COUNT(*),'|',SUM(ManualCaloriesKcal),'|',SUM(ISNULL(ManualProteinG,-1)),'|',SUM(ManualCarbsG),'|',SUM(ManualFatG)) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
    Check("0d1 Down TỪ CHỐI khi còn manual (item 10): ném 'S29 Down refused', KHÔNG drop cột nào, lịch sử migration vẫn ở S29, dữ liệu manual nguyên vẹn",
        refusal is not null && refusal.Contains("S29 Down refused") && colsAfterRefusal == 4 && lastAfterRefusal!.EndsWith("S29_ManualNutrition") && manualAfterRefusal == manualBefore,
        $"lỗi [{(refusal is null ? "không có" : refusal[..Math.Min(40, refusal.Length)])}], cột {colsAfterRefusal}, migration cuối {lastAfterRefusal}, manual nguyên vẹn {manualAfterRefusal == manualBefore}");
    await Exec("UPDATE FoodLogs SET ManualCaloriesKcal = NULL, ManualProteinG = NULL, ManualCarbsG = NULL, ManualFatG = NULL WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");   // SAU khi đã sao lưu (bảng backup ở trên): vận hành CHỦ ĐỘNG xoá giá trị
    using (var c = NewCtx()) await c.GetService<IMigrator>().MigrateAsync(s28);   // S29 Down (được phép: không còn giá trị manual)
    var colsAfterDown = await Scalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
    var ckAfterDown = await Scalar<int>("SELECT COUNT(*) FROM sys.check_constraints WHERE name = 'CK_FoodLogs_ManualNutrition_Range'");
    var fpDown = (await Scalar<string>(Fp()))!;
    var backupRows = await Scalar<int>("SELECT COUNT(*) FROM dbo.FoodLogManualBackup");
    Check("0d Rollback (Down) trên LocalDB: cột + CHECK biến mất, dữ liệu Food Log cũ (dòng, entry, nước, ghi chú, checksum) nguyên vẹn; GIÁ TRỊ MANUAL chỉ còn trong bảng sao lưu đã xuất trước đó",
        colsAfterDown == 0 && ckAfterDown == 0 && fpDown == fpBefore && backupRows == 1, $"cột {colsAfterDown}, CHECK {ckAfterDown}, dữ liệu cũ nguyên vẹn {fpDown == fpBefore}, dòng sao lưu {backupRows}");
    using (var c = NewCtx()) await c.Database.MigrateAsync();   // Up lại
    await Exec("UPDATE l SET l.ManualCaloriesKcal = b.ManualCaloriesKcal, l.ManualProteinG = b.ManualProteinG, l.ManualCarbsG = b.ManualCarbsG, l.ManualFatG = b.ManualFatG FROM FoodLogs l JOIN dbo.FoodLogManualBackup b ON b.Id = l.Id");
    var manualRestored = await Scalar<string>("SELECT CONCAT(COUNT(*),'|',SUM(ManualCaloriesKcal),'|',SUM(ISNULL(ManualProteinG,-1)),'|',SUM(ManualCarbsG),'|',SUM(ManualFatG)) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
    Check("0e Up lại + khôi phục từ sao lưu: giá trị manual (kể cả NULL và 0 riêng biệt) giống hệt trước rollback; dữ liệu Food Log cũ vẫn nguyên", manualBefore == manualRestored && (await Scalar<string>(Fp())) == fpBefore, $"trước {manualBefore} / sau khôi phục {manualRestored}");
    await Exec("DROP TABLE dbo.FoodLogManualBackup");
    await Exec("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");   // các phần sau dùng dữ liệu thử không cần user/food thật

    // =========================== 1) P0 giữ nguyên: Quick Edit KHÔNG đụng món; Clear/Delete NDM đúng phạm vi ===========================
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-3); await Seed(u, d, foods: 10, kcalEach: 180);
        var before = await Snapshot(u, d); var foodsBefore = await FoodItemCount(u);
        var r = await QuickEdit(u, d, 2200, p: 150, c: 250, f: 70);
        var after = await Snapshot(u, d); var day = await Day(u, d);
        Check("1 Quick Edit ngày có 10 món (P0 B1): cả 10 món + entry Id giữ nguyên, KHÔNG xoá mềm, không món/FoodItem mới; override lưu ở cột ngày; raw 1800, hiệu lực 2200",
            r.Status == 200 && after.Live == 10 && after.Deleted == 0 && after.TotalCal == 1800 && after.EntryIds == before.EntryIds && await FoodItemCount(u) == foodsBefore
            && after is { MCal: 2200m, MP: 150m, MC: 250m, MF: 70m } && day is { Calories: 2200m, ProteinG: 150m, Source: "Manual", HasOverride: true, FoodLogCalories: 1800m, EntryCount: 10 },
            $"status {r.Status}, live {after.Live}, deleted {after.Deleted}, raw {after.TotalCal}, manual {after.MCal}/{after.MP}/{after.MC}/{after.MF}, effective {day?.Calories} {day?.Source}");
    }
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-7);
        var logId = await Seed(u, d, foods: 3, kcalEach: 600, mk: 2500, waterMl: 750, note: true, incomplete: true);
        var r = await Clear(u, d); var s = await Snapshot(u, d); var again = await Clear(u, d); var s2 = await Snapshot(u, d); var day = await Day(u, d);
        Check("2 Clear Food Log Day: dòng ngày còn, 3 món xoá mềm, override + nước + ghi chú giữ; cờ chưa đủ giữ (ngày còn override ⇒ không trống); gọi lại không lỗi",
            r.Status == 200 && s.Exists && s.LogId == logId && s.Live == 0 && s.Deleted == 3 && s.TotalCal == 0 && s.MCal == 2500m && s.Water == 1 && s.Notes == 1 && s.Incomplete && s.LogNotes == "day note" && again.Status == 200 && s2 == s
            && day is { Calories: 2500m, Source: "Manual" },
            $"status {r.Status}, live {s.Live}, deleted {s.Deleted}, manual {s.MCal}, water {s.Water}, notes {s.Notes}, incomplete {s.Incomplete}, again {again.Status}");
        var u2 = Guid.NewGuid(); var d2 = today.AddDays(-8);
        var logId2 = await Seed(u2, d2, mk: 2200, waterMl: 500, note: true, incomplete: true);
        var del = await DeleteNdm(u2, d2); var s3 = await Snapshot(u2, d2);
        Check("3 Delete Nutrition Data ngày chỉ có override: xoá 4 cột; dòng ngày + nước + ghi chú còn; ngày thành trống thật ⇒ cờ chưa đủ TỰ BỎ (R2-4)",
            del.Status == 200 && s3.Exists && s3.LogId == logId2 && s3.MCal is null && s3.Water == 1 && s3.Notes == 1 && !s3.Incomplete && s3.LogNotes == "day note", $"status {del.Status}, manual {s3.MCal}, water {s3.Water}, notes {s3.Notes}, incomplete {s3.Incomplete}");
        var u3 = Guid.NewGuid(); var d3 = today.AddDays(-8); await Seed(u3, d3, foods: 3, kcalEach: 600, waterMl: 300);
        var b3 = await Snapshot(u3, d3); var del3 = await DeleteNdm(u3, d3);
        var del4 = await DeleteNdm(Guid.NewGuid(), d3);
        Check("3b Delete Nutrition Data ngày chỉ có món: 400 không xoá gì; ngày không tồn tại: 404", del3.Status == 400 && await Snapshot(u3, d3) == b3 && del4.Status == 404, $"foods-only {del3.Status}, missing {del4.Status}");
    }

    // =========================== 2) OVERRIDE THẮNG: thêm/sửa/xoá món khi override tồn tại; Remove Override; legacy Quick Add giữ nguyên ===========================
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-9);
        await Seed(u, d, foods: 3, kcalEach: 600);   // Food Log 1800
        var save = await Save(u, d, 2200, 150, 70, 250);
        var entriesBefore = await LiveEntryIds(u, d);
        var add = await AddFood(u, d, 300);
        var afterAdd = await Day(u, d); var sAdd = await Snapshot(u, d);
        var edit = await EditFoodGrams(u, add.Value, 200);   // món 300 kcal (100 g) → 200 g = 600 kcal
        var afterEdit = await Day(u, d);
        var del = await DeleteFood(u, add.Value);
        var afterDel = await Day(u, d); var sDel = await Snapshot(u, d);
        var rm = await RemoveOverride(u, d); var sRm = await Snapshot(u, d);
        Check("4 Override thắng: Food Log 1800 + manual 2200; thêm món 300 ⇒ raw 2100, hiệu lực vẫn 2200 (không 4000, không 2500); sửa món (raw 2400) hiệu lực vẫn 2200; xoá món ⇒ raw 1800; manual không bị đổi",
            save.Status == 200 && add.Status == 200 && afterAdd is { Calories: 2200m, FoodLogCalories: 2100m, Source: "Manual", EntryCount: 4 } && sAdd is { MCal: 2200m, MP: 150m, MC: 250m, MF: 70m } && edit.Status == 200 && afterEdit is { Calories: 2200m, FoodLogCalories: 2400m }
            && del.Status == 200 && afterDel is { Calories: 2200m, FoodLogCalories: 1800m } && sDel is { MCal: 2200m } && (await LiveEntryIds(u, d)).SequenceEqual(entriesBefore),   // sau khi thêm rồi xoá món, 3 món gốc vẫn nguyên (cùng Id)
            $"save {save.Status}, add {add.Status} → eff {afterAdd?.Calories}/raw {afterAdd?.FoodLogCalories}, edit {edit.Status} → eff {afterEdit?.Calories}/raw {afterEdit?.FoodLogCalories}, delete {del.Status} → eff {afterDel?.Calories}/raw {afterDel?.FoodLogCalories}");
        Check("5 Remove Override: hiệu lực quay về Food Log (1800), 3 món + nước/ghi chú giữ, 4 cột manual NULL; lần hai idempotent; ngày không có = 404",
            rm.Status == 200 && rm.Value is { Calories: 1800m, Source: "FoodLog", HasOverride: false, ManualCaloriesKcal: null } && sRm.MCal is null && sRm.MP is null && sRm.Live == 3
            && (await RemoveOverride(u, d)).Status == 200 && (await RemoveOverride(Guid.NewGuid(), d)).Status == 404,
            $"status {rm.Status}, eff {rm.Value?.Calories} {rm.Value?.Source}, live {sRm.Live}, manual {sRm.MCal}");
    }
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-10);
        await Seed(u, d, foods: 2, kcalEach: 500, legacyQuickAdd: 400);   // Quick Add cũ = entry bình thường (tổng 1400)
        var legacyBefore = await Snapshot(u, d);
        var s = await Save(u, d, 900, 80, 40, 100);
        var sOver = await Snapshot(u, d);
        var rm = await RemoveOverride(u, d); var sRm = await Snapshot(u, d); var day = await Day(u, d);
        Check("6 Quick Add cũ (Source=QuickAdd) được GIỮ NGUYÊN: không chuyển đổi, không xoá, không coi là override; manual 900 thắng; gỡ override ⇒ 1400 từ các entry (gồm Quick Add cũ)",
            s.Status == 200 && sOver.EntryIds == legacyBefore.EntryIds && sOver.Live == 3 && sOver.Deleted == 0 && sOver.TotalCal == 1400 && s.Value is { Calories: 900m, Source: "Manual", EntryCount: 3, FoodLogCalories: 1400m }
            && rm.Status == 200 && sRm.EntryIds == legacyBefore.EntryIds && day is { Calories: 1400m, Source: "FoodLog" }, $"save {s.Status}, entries kept {sOver.EntryIds == legacyBefore.EntryIds}, after remove eff {day?.Calories} {day?.Source}");
    }

    // =========================== 3) NULL vs 0, calories-only, macro-only, partial, Save không thành công khi không đổi ===========================
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-11);
        await Seed(u, d, foods: 2, kcalEach: 500);   // Food Log có macro (20/40/10): manual null KHÔNG được lấy từ đây
        var kcalOnly = await Save(u, d, 2200); var sK = await Snapshot(u, d);
        var withZero = await Save(u, d, 2200, p: 0); var dayZero = await Day(u, d);
        var partial = await Save(u, d, 2200, p: 150, c: 250); var dayPartial = await Day(u, d);
        Check("7 Calories-only ⇒ macro NULL (không 0, không lấy từ Food Log); protein 0 nhập tay ⇒ 0 (khác null); partial giữ đúng các trường đã nhập, trường thiếu vẫn null",
            kcalOnly.Value is { Calories: 2200m, ProteinG: null, FatG: null, CarbsG: null, ManualProteinG: null } && sK is { MCal: 2200m, MP: null, MC: null, MF: null } && withZero.Status == 200 && partial.Status == 200
            && dayZero is { ProteinG: 0m, FatG: null, CarbsG: null } && dayPartial is { ProteinG: 150m, CarbsG: 250m, FatG: null, Calories: 2200m } && dayPartial.FoodLogProteinG == 20m,
            $"kcal-only {kcalOnly.Value?.ProteinG?.ToString() ?? "null"}, zero {dayZero?.ProteinG}, partial {dayPartial?.ProteinG}/{dayPartial?.CarbsG}/{dayPartial?.FatG?.ToString() ?? "null"}");
    }
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-12);
        await Seed(u, d, foods: 2, kcalEach: 500);
        var proteinOnly = await Save(u, d, p: 150); var fatOnly = await Save(u, d, f: 60); var carbsOnly = await Save(u, d, c: 200); var day = await Day(u, d);
        var days = await Days(u, d, d); var ld = days[d];
        Check("8 Macro-only được phép (R2-3): kcal NULL (không 4P+4C+9F, không lấy 1000 của Food Log), chỉ trường đã nhập có giá trị, KHÔNG tham gia thuật toán; chỉ-protein/chỉ-fat/chỉ-carbs đều lưu được",
            proteinOnly.Status == 200 && fatOnly.Status == 200 && carbsOnly.Status == 200 && day is { Calories: null, ProteinG: null, FatG: null, CarbsG: 200m, HasOverride: true, IsValidForAlgorithms: false, FoodLogCalories: 1000m } && ld.AlgorithmKcal is null && ld.Calories is null,
            $"statuses {proteinOnly.Status}/{fatOnly.Status}/{carbsOnly.Status}, eff kcal {day?.Calories?.ToString() ?? "null"}, carbs {day?.CarbsG}, algorithm kcal {ld.AlgorithmKcal?.ToString() ?? "null"}");
    }
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-13);
        await Seed(u, d, foods: 1, kcalEach: 700, mk: 2200, mp: 150);
        var b = await Snapshot(u, d);
        var clearAll = await Save(u, d);                          // xoá trống tất cả rồi Save: không xoá override, không thành công
        var same = await Save(u, d, 2200, p: 150);                // đúng số đang lưu: idempotent, không ghi
        var afterSame = await Snapshot(u, d);
        var emptyDay = Guid.NewGuid(); var d2 = today.AddDays(-13);
        var nothing = await Save(emptyDay, d2);
        Check("9 Xoá trống tất cả trường rồi Save (R2-3): 400 USE_REMOVE_OVERRIDE, KHÔNG xoá override, KHÔNG báo thành công; ngày không có gì: 400 NOTHING_TO_SAVE, không tạo dòng; lưu đúng số đang có = 200 không ghi",
            clearAll is { Status: 400, Reason: "USE_REMOVE_OVERRIDE" } && same.Status == 200 && afterSame == b && nothing is { Status: 400, Reason: "NOTHING_TO_SAVE" } && !(await Snapshot(emptyDay, d2)).Exists,
            $"clear-all {clearAll.Status}:{clearAll.Reason}, same {same.Status} unchanged {afterSame == b}, nothing {nothing.Status}:{nothing.Reason}, row created {(await Snapshot(emptyDay, d2)).Exists}");
    }

    // =========================== 4) NHỊN ĂN: xung đột + manual 0 + Missing/Incomplete (R2-2, R2-3.4) ===========================
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-14);
        var fastOn = await SetFasting(u, d, true); var b1 = await Snapshot(u, d);
        var kcalOnFast = await Save(u, d, 500); var macroOnFast = await Save(u, d, p: 20); var qe = await QuickEdit(u, d, 300); var b2 = await Snapshot(u, d);
        var zero = await Save(u, d, 0, 0, 0, 0);                  // nhịn ăn + manual 0: hợp lệ
        var dayZero = await Day(u, d); var zeroDays = await Days(u, d, d);
        var keep = await Save(u, d, 0, incomplete: true);          // IsFasting null = giữ nhịn ăn
        var sKeep = await Snapshot(u, d);
        Check("10 Nhịn ăn + manual dương (calories HAY macro) ⇒ 400 FASTING_WITH_MANUAL_NUTRITION, không tự bỏ nhịn ăn, không ghi manual; manual 0 + nhịn ăn hợp lệ (0 kcal vào thuật toán); cờ null = giữ nguyên",
            fastOn.Status == 200 && b1.Fasting && kcalOnFast is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && macroOnFast is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && qe is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" }
            && b2 == b1 && zero.Status == 200 && dayZero is { Calories: 0m, IsFasting: true, IsValidForAlgorithms: true, Conflict: null } && zeroDays[d].AlgorithmKcal == 0m && keep.Status == 200 && sKeep.Fasting && sKeep.Incomplete,
            $"fasting {fastOn.Status}, kcal {kcalOnFast.Status}:{kcalOnFast.Reason}, macro {macroOnFast.Status}, quickEdit {qe.Status}, unchanged {b2 == b1}, zero {zero.Status} algo {zeroDays[d].AlgorithmKcal}, keep {keep.Status} fasting {sKeep.Fasting}");
        var u2 = Guid.NewGuid(); var d2 = today.AddDays(-14);
        await Seed(u2, d2, mk: 700);                              // override dương rồi bật nhịn ăn
        var onWithManual = await SetFasting(u2, d2, true); var onViaSave = await Save(u2, d2, fasting: true);
        var u3 = Guid.NewGuid(); await Seed(u3, d2, foods: 2, kcalEach: 400);
        var onWithFood = await SetFasting(u3, d2, true);
        var explicitOff = await Save(u, d, 600, fasting: false);   // bỏ nhịn ăn RÕ RÀNG trong cùng request thì được
        var sOff = await Snapshot(u, d);
        Check("10b Bật nhịn ăn trên ngày có override dương ⇒ 400; trên ngày có món ⇒ 409 FASTING_NEEDS_EMPTY_DAY; tắt nhịn ăn RÕ RÀNG (isFasting=false) kèm manual thì được (không tự bỏ ngầm)",
            onWithManual is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && onViaSave is { Status: 400 } && onWithFood is { Status: 409, Reason: "FASTING_NEEDS_EMPTY_DAY" } && explicitOff.Status == 200 && !sOff.Fasting && sOff.MCal == 600m,
            $"manual {onWithManual.Status}:{onWithManual.Reason}, via save {onViaSave.Status}, food {onWithFood.Status}:{onWithFood.Reason}, explicit off {explicitOff.Status} fasting {sOff.Fasting}");
    }
    {
        // R2-2: tám trạng thái KHÁC NHAU không bị đánh đồng (Missing / manual 0 / Fasting 0 / Incomplete / entry 0 kcal / manual 2000 / macro-only / Food Log 1800)
        var u = Guid.NewGuid(); var b = today.AddDays(-30);
        var dMissing = b; var dZero = b.AddDays(1); var dFast = b.AddDays(2); var dInc = b.AddDays(3); var dEntry0 = b.AddDays(4); var dManual = b.AddDays(5); var dMacro = b.AddDays(6); var dFood = b.AddDays(7);
        await Seed(u, dZero, mk: 0); await Seed(u, dFast, fasting: true); await Seed(u, dInc, foods: 1, kcalEach: 1500, incomplete: true); await Seed(u, dEntry0, foods: 1, kcalEach: 0);
        await Seed(u, dManual, mk: 2000); await Seed(u, dMacro, mp: 120); await Seed(u, dFood, foods: 3, kcalEach: 600);
        var all = await Days(u, b, b.AddDays(7));
        decimal? A(DateOnly x) => FoodLogSupport.AlgorithmKcalOn(all, x);
        Check("11 Điều kiện tham gia thuật toán (R2-2): Missing = null (vắng), manual 0 không Fasting = null (lưu 0), Fasting 0 = 0, Incomplete = null, entry 0 kcal = null, manual 2000 = 2000, macro-only = null, Food Log 1800 = 1800",
            !all.ContainsKey(dMissing) && A(dMissing) is null && A(dZero) is null && all[dZero].Calories == 0m && A(dFast) == 0m && A(dInc) is null && all[dInc].Calories == 1500m && A(dEntry0) is null && A(dManual) == 2000m && A(dMacro) is null && all[dMacro].Protein == 120m && A(dFood) == 1800m,
            $"missing {all.ContainsKey(dMissing)}, manual0 {A(dZero)?.ToString() ?? "null"}, fasting {A(dFast)}, incomplete {A(dInc)?.ToString() ?? "null"}, entry0 {A(dEntry0)?.ToString() ?? "null"}, manual2000 {A(dManual)}, macroOnly {A(dMacro)?.ToString() ?? "null"}, food {A(dFood)}");
        var eb = EnergyBalanceCalculator.Days(b, b.AddDays(7), all, Enumerable.Range(0, 8).ToDictionary(i => b.AddDays(i), _ => 2000m), _ => 1900m);
        Check("11b Energy Balance không chênh lệch giả: manual 0 không Fasting hiện 0 nhưng vs = null; macro-only kcal null; Incomplete/entry 0 kcal vs = null; manual 2000 vs = 0; Fasting 0 vs = −2000",
            eb[1] is { CaloriesKcal: 0m, VsExpenditureKcal: null } && eb[2] is { CaloriesKcal: 0m, VsExpenditureKcal: -2000m } && eb[3].VsExpenditureKcal is null && eb[4].VsExpenditureKcal is null && eb[5] is { CaloriesKcal: 2000m, VsExpenditureKcal: 0m } && eb[6] is { CaloriesKcal: null, VsExpenditureKcal: null } && eb[0].CaloriesKcal is null,
            string.Join(" ", eb.Select(x => $"{x.CaloriesKcal?.ToString() ?? "-"}/{x.VsExpenditureKcal?.ToString() ?? "-"}")));
        var uC = Guid.NewGuid(); await Seed(uC, dFood, foods: 3, kcalEach: 600);
        var zeroOverFood = await Save(uC, dFood, 0); var conflictDay = await Day(uC, dFood); var sFood = await Snapshot(uC, dFood);
        Check("11c Manual 0 kcal (không Fasting) + Food Log dương = XUNG ĐỘT chỉ báo cáo: override thắng (0), không xoá món, không tự đánh dấu Fasting, ngày không tham gia thuật toán, conflict = MANUAL_ZERO_WITH_FOOD_LOG",
            zeroOverFood.Status == 200 && conflictDay is { Calories: 0m, FoodLogCalories: 1800m, IsFasting: false, IsValidForAlgorithms: false, Conflict: "MANUAL_ZERO_WITH_FOOD_LOG", EntryCount: 3 } && sFood is { Live: 3, Deleted: 0, Fasting: false },
            $"status {zeroOverFood.Status}, eff {conflictDay?.Calories}, raw {conflictDay?.FoodLogCalories}, conflict {conflictDay?.Conflict}, valid {conflictDay?.IsValidForAlgorithms}");
    }

    // =========================== 5) R2-4: tự bỏ cờ chưa-đủ CHỈ khi ngày trống thật ===========================
    {
        async Task<bool> Flag(Guid u, DateOnly d) => (await Snapshot(u, d)).Incomplete;
        var n = 0; DateOnly Nd() => today.AddDays(-40 - n++);
        // xoá món cuối ⇒ bỏ cờ; xoá một trong nhiều ⇒ giữ
        var u1 = Guid.NewGuid(); var d1 = Nd(); var a1 = await AddFood(u1, d1, 300); var b1 = await AddFood(u1, d1, 200); await SetIncomplete(u1, d1, true);
        var del1 = await DeleteFood(u1, a1.Value); var keep1 = await Flag(u1, d1); var del1b = await DeleteFood(u1, b1.Value); var clr1 = await Flag(u1, d1);
        var re = await AddFood(u1, d1, 500); var reDays = await Days(u1, d1, d1);   // thêm lại sau khi trống: không bị loại khỏi thuật toán
        Check("12 Delete Food: xoá 1 trong 2 món GIỮ cờ chưa-đủ; xoá món CUỐI ⇒ cờ tự bỏ (cùng giao dịch); log lại sau đó không bị loại khỏi thuật toán (AlgorithmKcal = 500)",
            del1.Status == 200 && keep1 && del1b.Status == 200 && !clr1 && re.Status == 200 && reDays[d1] is { IsIncomplete: false, AlgorithmKcal: 500m }, $"keep {keep1}, cleared {!clr1}, relog algo {reDays[d1].AlgorithmKcal}");
        // Bulk Delete, Clear Food Log
        var u2 = Guid.NewGuid(); var d2 = Nd(); var e2 = new[] { (await AddFood(u2, d2, 100)).Value, (await AddFood(u2, d2, 200)).Value, (await AddFood(u2, d2, 300)).Value }; await SetIncomplete(u2, d2, true);
        await BulkDelete(u2, e2[0], e2[1]); var bulkKeep = await Flag(u2, d2); await BulkDelete(u2, e2[2]); var bulkClr = await Flag(u2, d2);
        var u3 = Guid.NewGuid(); var d3 = Nd(); await AddFood(u3, d3, 100); await AddFood(u3, d3, 150); await SetIncomplete(u3, d3, true);
        await Clear(u3, d3); var clearClr = await Flag(u3, d3);
        Check("12b Bulk Delete (xoá một phần GIỮ cờ, xoá hết ⇒ bỏ cờ) và Clear Food Log (ngày hết món, không override ⇒ bỏ cờ)", bulkKeep && !bulkClr && !clearClr, $"bulk partial keeps {bulkKeep}, bulk all clears {!bulkClr}, clear clears {!clearClr}");
        // Remove Override: override cuối bị gỡ ⇒ bỏ cờ; còn món ⇒ giữ
        var u4 = Guid.NewGuid(); var d4 = Nd(); await Seed(u4, d4, mk: 2200, incomplete: true);
        var rm4 = await RemoveOverride(u4, d4); var rm4Flag = await Flag(u4, d4);
        var u5 = Guid.NewGuid(); var d5 = Nd(); await Seed(u5, d5, foods: 1, kcalEach: 300, mk: 2200, incomplete: true);
        await RemoveOverride(u5, d5); var rm5Flag = await Flag(u5, d5);
        Check("12c Remove Override: gỡ override cuối ⇒ ngày trống thật ⇒ bỏ cờ; còn món ⇒ GIỮ cờ", rm4.Status == 200 && !rm4Flag && rm5Flag, $"last override clears {!rm4Flag}, with food keeps {rm5Flag}");
        // Ngày KHÔNG trống thật: thêm rồi xoá một món (kích hoạt kiểm tra dọn cờ) mà cờ vẫn còn
        var keepers = new (string Name, Func<Guid, DateOnly, Task> Make, bool Cycle)[]
        {
            ("calories-only", (u, d) => Seed(u, d, mk: 2000, incomplete: true), true), ("macro-only", (u, d) => Seed(u, d, mp: 100, incomplete: true), true), ("manual 0", (u, d) => Seed(u, d, mk: 0, incomplete: true), true),
            ("entry 0 kcal", (u, d) => Seed(u, d, foods: 1, kcalEach: 0, incomplete: true), true), ("fasting", (u, d) => Seed(u, d, fasting: true, incomplete: true), false),
        };
        var kept = new List<string>();
        foreach (var (name, make, cycle) in keepers)
        {
            var uk = Guid.NewGuid(); var dk = Nd(); await make(uk, dk);
            if (cycle) { var f = await AddFood(uk, dk, 100); await DeleteFood(uk, f.Value); } else await SetFasting(uk, dk, true);   // thao tác chạy bước dọn cờ nhưng ngày vẫn có nguồn hợp lệ
            if (await Flag(uk, dk)) kept.Add(name);
        }
        Check("12d Ngày KHÔNG trống thật vẫn giữ cờ chưa-đủ: calories-only, macro-only, manual 0, entry 0 kcal (không suy ra trống từ TotalCalories = 0), nhịn ăn", kept.Count == 5, $"giữ cờ: {string.Join(", ", kept)}");
        // ghi chú và nước không làm ngày "có dữ liệu dinh dưỡng"
        var u6 = Guid.NewGuid(); var d6 = Nd(); await Seed(u6, d6, foods: 1, kcalEach: 100, waterMl: 200, note: true, incomplete: true);
        var only6 = (await LiveEntryIds(u6, d6)).Single(); await DeleteFood(u6, only6); var s6 = await Snapshot(u6, d6);
        Check("12e Ngày có ghi chú + nước nhưng hết món ⇒ vẫn là ngày trống thật ⇒ cờ bỏ; ghi chú/nước/dòng ngày không bị đụng", s6 is { Exists: true, Incomplete: false, Water: 1, Notes: 1, Live: 0 }, $"incomplete {s6.Incomplete}, water {s6.Water}, notes {s6.Notes}");
        // đồng thời: xoá món cuối song song thêm món
        var violations = 0;
        for (var i = 0; i < 6; i++)
        {
            var uc = Guid.NewGuid(); var dc = Nd(); var only = await AddFood(uc, dc, 100); await SetIncomplete(uc, dc, true);
            var rs = await Task.WhenAll(St(DeleteFood(uc, only.Value)), St(AddFood(uc, dc, 250)));
            var sc = await Snapshot(uc, dc);
            if (rs.Any(r => r.Status is not (200 or 409)) || (sc.Live == 0 && sc.Incomplete)) violations++;
        }
        Check("12f Đồng thời xoá món cuối + thêm món (6 vòng): không 500, KHÔNG BAO GIỜ còn ngày trống mang cờ chưa-đủ", violations == 0, $"vi phạm {violations}/6");
    }

    // =========================== 6) DẪN XUẤT: DerivedDataSync + DailyProgress dùng số HIỆU LỰC ===========================
    {
        var u = Guid.NewGuid(); var start = today.AddDays(-8); var d = today.AddDays(-5);
        await SeedProgram(u, start);
        await AddFood(u, d, 1800);
        var p0 = await DailyProgress(u, d);
        await Save(u, d, 2200, 150, 70, 250); var p1 = await DailyProgress(u, d);
        await AddFood(u, d, 300); var p2 = await DailyProgress(u, d);
        await RemoveOverride(u, d); var p3 = await DailyProgress(u, d);
        await Save(u, d, p: 100); var p4 = await DailyProgress(u, d);
        Check("13 Derived freshness: DailyProgress theo ngày = Food Log 1800 → manual 2200 → thêm món vẫn 2200 → gỡ override 2100 (rebuild chạy sau mỗi thao tác); macro-only (kcal chưa biết) KHÔNG lưu thành 0 'đã ăn' (IsLogged = false)",
            p0 is (1800m, true) && p1 is (2200m, true) && p2 is (2200m, true) && p3 is (2100m, true) && p4.Logged == false, $"{p0} → {p1} → {p2} → {p3} → macro-only {p4}");
    }

    // =========================== 7) HỢP ĐỒNG v2: danh sách NDM, phân trang, Food Log, week strip ===========================
    {
        var u = Guid.NewGuid(); var b = today.AddDays(-60);
        await Seed(u, b, foods: 2, kcalEach: 500); await Seed(u, b.AddDays(1), mk: 2200, mp: 150); await Seed(u, b.AddDays(2), fasting: true); await Seed(u, b.AddDays(3), mp: 80);
        await Seed(u, b.AddDays(4), foods: 1, kcalEach: 700, mk: 0); await Seed(u, b.AddDays(5), note: true);   // ngày chỉ có dòng (ghi chú) KHÔNG xuất hiện
        var list = (await NdmList(u)).Value!;
        var byDate = list.Items.ToDictionary(i => i.Date);
        Check("14 Hợp đồng v2 GET /nutrition-data: nguồn Manual|FoodLog|Fasting, hiệu lực nullable, phần manual, raw Food Log, isValidForAlgorithms, conflict; ngày chỉ có ghi chú/không dữ liệu vắng mặt (Unlogged), không 0 giả",
            list.Items.Count == 5 && byDate[b] is { Source: "FoodLog", Calories: 1000m, HasOverride: false, IsValidForAlgorithms: true, EntryCount: 2 } && byDate[b.AddDays(1)] is { Source: "Manual", Calories: 2200m, ProteinG: 150m, FatG: null, CarbsG: null, ManualProteinG: 150m, FoodLogCalories: 0m, EntryCount: 0 }
            && byDate[b.AddDays(2)] is { Source: "Fasting", Calories: 0m, IsFasting: true, IsValidForAlgorithms: true } && byDate[b.AddDays(3)] is { Source: "Manual", Calories: null, ProteinG: 80m, IsValidForAlgorithms: false }
            && byDate[b.AddDays(4)] is { Source: "Manual", Calories: 0m, FoodLogCalories: 700m, IsValidForAlgorithms: false, Conflict: "MANUAL_ZERO_WITH_FOOD_LOG" } && !byDate.ContainsKey(b.AddDays(5)) && list.Items.Select(i => i.Date).SequenceEqual(list.Items.Select(i => i.Date).OrderByDescending(x => x)),
            $"{list.Items.Count} ngày: " + string.Join(" | ", list.Items.AsEnumerable().Reverse().Select(i => $"{i.Source}/{i.Calories?.ToString() ?? "null"}")));
        var p1 = (await NdmList(u, limit: 2)).Value!; var p2 = (await NdmList(u, limit: 2, before: p1.NextBefore)).Value!; var p3 = (await NdmList(u, limit: 2, before: p2.NextBefore)).Value!;
        Check("14b Phân trang: limit 2 ⇒ hasMore + nextBefore = ngày cũ nhất của trang; trang kế tiếp không trùng; trang cuối hasMore = false; tổng 5 ngày", p1 is { HasMore: true } && p2 is { HasMore: true } && p3 is { HasMore: false, NextBefore: null }
            && p1.Items.Count == 2 && p2.Items.Count == 2 && p3.Items.Count == 1 && p1.NextBefore == p1.Items[^1].Date && p1.Items.Concat(p2.Items).Concat(p3.Items).Select(i => i.Date).Distinct().Count() == 5,
            $"trang {p1.Items.Count}/{p2.Items.Count}/{p3.Items.Count}, hasMore {p1.HasMore}/{p2.HasMore}/{p3.HasMore}");

        var u2 = Guid.NewGuid(); var d2 = today.AddDays(-2); await Seed(u2, d2, foods: 3, kcalEach: 600, mk: 2200); var d3 = today.AddDays(-1); await Seed(u2, d3, mk: 1500, mp: 100);
        var dl = await Call(uow => new GetDayLogQueryHandler(Clock(uow, u2), uow, new TestUser(u2), TimeProvider.System).Handle(new GetDayLogQuery(d2), default));
        var dl3 = await Call(uow => new GetDayLogQueryHandler(Clock(uow, u2), uow, new TestUser(u2), TimeProvider.System).Handle(new GetDayLogQuery(d3), default));
        var wk = await Call(uow => new GetWeekLogQueryHandler(Clock(uow, u2), uow, new TestUser(u2), TimeProvider.System).Handle(new GetWeekLogQuery(d2), default));
        var wDay = wk.Value!.Days.First(x => x.LogDate == d2); var wDay3 = wk.Value.Days.FirstOrDefault(x => x.LogDate == d3);
        Check("15 GET /food-logs/{date} và week strip: Totals = RAW món (1800), Effective = hiệu lực (2200, Manual); ngày chỉ có manual (không món) vẫn IsLogged với Effective",
            dl.Value is { IsLogged: true } && dl.Value.Totals.Calories == 1800m && dl.Value.Effective is { Calories: 2200m, Source: "Manual", FoodLogCalories: 1800m } && dl3.Value is { IsLogged: true } && dl3.Value.Entries.Count == 0 && dl3.Value.Effective is { Calories: 1500m, ProteinG: 100m, FatG: null }
            && wDay is { IsLogged: true } && wDay.Totals.Calories == 1800m && wDay.Effective is { Calories: 2200m } && (wDay3 is null || (wDay3.IsLogged && wDay3.Effective is { Calories: 1500m })),
            $"day totals {dl.Value?.Totals.Calories} eff {dl.Value?.Effective?.Calories}, manual-only logged {dl3.Value?.IsLogged}, week eff {wDay.Effective?.Calories}");
    }

    // =========================== 8) ĐỒNG THỜI + GIAO DỊCH HỎNG + LỊCH SỬ ===========================
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-11); await Seed(u, d, foods: 6, kcalEach: 200, waterMl: 100);
        var rs = await Task.WhenAll(Clear(u, d), Clear(u, d)); var s = await Snapshot(u, d);
        Check("16a Hai Clear đồng thời: không 500, 6 món xoá mềm đúng một lần, nước + ghi chú còn", rs.All(r => r.Status is 200 or 409) && rs.Any(r => r.Status == 200) && s.Live == 0 && s.Deleted == 6 && s.Water == 1 && s.Notes == 1, $"statuses {Statuses(rs)}, live {s.Live}, deleted {s.Deleted}");
        var u2 = Guid.NewGuid(); var d2 = today.AddDays(-11); await Seed(u2, d2, foods: 2, kcalEach: 300);
        var qs = await Task.WhenAll(St(Save(u2, d2, 2100)), St(Save(u2, d2, 2300)), St(QuickEdit(u2, d2, 2500)));
        var s2 = await Snapshot(u2, d2); var day2 = await Day(u2, d2);
        Check("16b Ba lần ghi override đồng thời: không 500, món nguyên vẹn, đúng một giá trị cuối hợp lệ, không cộng", qs.All(r => r.Status is 200 or 409) && qs.Any(r => r.Status == 200) && s2.Live == 2 && s2.Deleted == 0 && (s2.MCal is 2100m or 2300m or 2500m) && day2?.Calories == s2.MCal,
            $"statuses {StatusList(qs)}, manual {s2.MCal}, live {s2.Live}");
        var u3 = Guid.NewGuid(); var d3 = today.AddDays(-12); await Seed(u3, d3, foods: 3, kcalEach: 400, mk: 1800);
        var mixed = await Task.WhenAll(St(RemoveOverride(u3, d3)), St(AddFood(u3, d3, 100)), St(Save(u3, d3, 2000)));
        var s3 = await Snapshot(u3, d3); var day3 = await Day(u3, d3);
        Check("16c Remove Override song song thêm món và Save: không 500, không món nào bị mất (≥ 3 món live), hiệu lực khớp nguồn của trạng thái cuối", mixed.All(r => r.Status is 200 or 409) && s3.Live >= 3 && s3.Deleted == 0 && day3 is not null && (day3.HasOverride ? day3.Calories == s3.MCal : day3.Calories == s3.TotalCal),
            $"statuses {StatusList(mixed)}, live {s3.Live}, manual {s3.MCal}, eff {day3?.Calories}, raw {s3.TotalCal}");
    }
    {
        var u = Guid.NewGuid(); var d = today.AddDays(-12); await Seed(u, d, foods: 2, kcalEach: 500, mk: 1500, waterMl: 100);
        var before = await Snapshot(u, d); var foodsBefore = await FoodItemCount(u);
        var f1 = new FailOnUpdate("FoodLogs"); var f2 = new FailOnUpdate("FoodLogs"); var f3 = new FailOnUpdate("FoodLogs");
        var r1 = await Save(u, d, 2600, make: () => new ApplicationDbContext(Opts(f1)));
        var r2 = await QuickEdit(u, d, 2600, make: () => new ApplicationDbContext(Opts(f2)));
        var r3 = await Call(uow => new RemoveManualOverrideCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new RemoveManualOverrideCommand(d), default), () => new ApplicationDbContext(Opts(f3)));
        var after = await Snapshot(u, d);
        Check("17 Lỗi giữa SaveChanges (UPDATE FoodLogs): Save, Quick Edit, Remove Override đều trả lỗi, KHÔNG đổi gì (manual, món, cờ giữ nguyên), không tạo món",
            f1.Fired && f2.Fired && f3.Fired && r1.Status == 500 && r2.Status == 500 && r3.Status == 500 && after == before && await FoodItemCount(u) == foodsBefore, $"fired {f1.Fired}/{f2.Fired}/{f3.Fired}, {r1.Status}/{r2.Status}/{r3.Status}, unchanged {after == before}");
    }
    {
        var u = Guid.NewGuid(); var a = today.AddDays(-400); var b = today.AddDays(-1);
        await Seed(u, a, foods: 4, kcalEach: 300, legacyQuickAdd: 100, waterMl: 250); await Seed(u, b, foods: 5, kcalEach: 400, mk: 2500, waterMl: 600);
        var bBefore = await Snapshot(u, b);
        var sv = await Save(u, a, 2000, 120, 50, 240); var cl = await Clear(u, a); var rm = await RemoveOverride(u, a); var sa = await Snapshot(u, a); var bAfter = await Snapshot(u, b);
        Check("18 Ngày lịch sử (400 ngày trước): Save override / Clear / Remove Override trên ngày A không đụng ngày B; A: 5 entry (gồm Quick Add cũ) xoá mềm bởi Clear, nước + ghi chú còn, override đã gỡ",
            sv.Status == 200 && cl.Status == 200 && rm.Status == 200 && bAfter == bBefore && sa is { Live: 0, Deleted: 5, Water: 1, Notes: 1, MCal: null }, $"save {sv.Status} clear {cl.Status} remove {rm.Status}, B unchanged {bAfter == bBefore}, A live {sa.Live} deleted {sa.Deleted} water {sa.Water}");
    }

    // =========================== 9) ROUND 3: 409 CONCURRENT thật, phân trang 90 ngày qua lịch sử dài, Macro Chart calories-only trên DB ===========================
    {
        var direct = await Map(new DbUpdateConcurrencyException("x"));
        using var dj = JsonDocument.Parse(direct.Body);
        var root = dj.RootElement;
        var shapeOk = root.GetProperty("statusCode").GetInt32() == 409 && !root.GetProperty("isSuccess").GetBoolean() && root.GetProperty("message").GetString() == ExceptionMessages.ConcurrentWriteConflict
            && root.GetProperty("errors").GetProperty("reasonCode")[0].GetString() == "CONCURRENT";
        string? raceReason = null; var raceTries = 0;
        while (raceReason is null && raceTries++ < 15)
        {
            var u = Guid.NewGuid(); var d = today.AddDays(-20); await Seed(u, d, foods: 2, kcalEach: 300);
            var rs = await Task.WhenAll(St(Save(u, d, 2100)), St(Save(u, d, 2300)));
            raceReason = rs.FirstOrDefault(r => r.Status == 409).Reason;
        }
        Check("19 409 do xung đột ghi đồng thời mang errors.reasonCode = CONCURRENT qua GlobalExceptionHandler THẬT (cùng ApiResponse: isSuccess=false, statusCode 409, message cũ, errors.reasonCode); cũng với cuộc đua thật giữa hai Save",
            direct is { Status: 409, Reason: "CONCURRENT" } && shapeOk && raceReason == "CONCURRENT", $"direct {direct.Status}:{direct.Reason}, shape {shapeOk}, race reason {raceReason ?? "(chưa gặp đua sau 15 lần)"} sau {raceTries} lần");
    }
    {
        // 200 ngày, ngày chẵn = manual, ngày lẻ = có món; múi giờ +14 không đổi ngày trả về; trang mặc định 90, cuộn tiếp bằng con trỏ tới ngày cũ nhất
        var u = Guid.NewGuid(); var start = today.AddDays(-300);
        using (var c = NewCtx())
        {
            c.Add(new UserProfile { Id = Guid.NewGuid(), UserId = u, FirstName = "T", LastName = "T", TimeZone = "Pacific/Kiritimati" });
            for (var i = 0; i < 200; i++)
            {
                var id = Guid.NewGuid();
                c.Add(new FoodLog { Id = id, UserId = u, LogDate = start.AddDays(i), TotalCalories = i % 2 == 0 ? 0 : 500, ManualCaloriesKcal = i % 2 == 0 ? 2000 : null });
                if (i % 2 == 1) c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = id, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = 1, Calories = 500 });
            }
            await c.SaveChangesAsync();
        }
        var pages = new List<NutritionDataResponse>(); DateOnly? cursor = null;
        do { var p = (await NdmList(u, before: cursor)).Value!; pages.Add(p); cursor = p.NextBefore; } while (cursor is not null && pages.Count < 10);
        var allDates = pages.SelectMany(p => p.Items).Select(i => i.Date).ToList();
        var expected = Enumerable.Range(0, 200).Select(i => start.AddDays(i)).OrderByDescending(x => x).ToList();
        var w = (await NdmList(u, from: start.AddDays(50), to: start.AddDays(149))).Value!; var w2 = (await NdmList(u, from: start.AddDays(50), to: start.AddDays(149), before: w.NextBefore)).Value!;
        Check("20 Phân trang: trang mặc định 90 ngày KHÔNG phải giới hạn lịch sử; 200 ngày = 90/90/20 qua con trỏ, không trùng, giảm dần liên tục, tới tận ngày cũ nhất; ngày trả về đúng LogDate (múi giờ +14 không đổi ngày); from/to + con trỏ cùng hoạt động",
            pages.Select(p => p.Items.Count).SequenceEqual([90, 90, 20]) && pages.Select(p => p.HasMore).SequenceEqual([true, true, false]) && allDates.SequenceEqual(expected) && allDates.Distinct().Count() == 200 && allDates[^1] == start
            && pages[0].NextBefore == pages[0].Items[^1].Date && pages[2].NextBefore is null && w.Items.Count == 90 && w.HasMore && w2.Items.Count == 10 && !w2.HasMore && w.Items.Concat(w2.Items).Select(i => i.Date).Distinct().Count() == 100,
            $"trang {string.Join("/", pages.Select(p => p.Items.Count))}, tổng {allDates.Count}, khớp thứ tự {allDates.SequenceEqual(expected)}, cửa sổ {w.Items.Count}+{w2.Items.Count}");
    }
    {
        var u = Guid.NewGuid(); var b = today.AddDays(-6);
        await Seed(u, b, mk: 2000, mp: 150, mc: 200, mf: 60); await Seed(u, b.AddDays(1), mk: 2200); await Seed(u, b.AddDays(2), foods: 2, kcalEach: 900); await Seed(u, b.AddDays(3), mp: 90);
        var ms = await Call(uow => new GetMacroSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System).Handle(new GetMacroSeriesQuery(b, b.AddDays(5)), default));
        var ds = ms.Value!.Days.ToDictionary(x => x.Date); var avg = ms.Value.Summary.Averages;
        Check("21 Macro Chart trên DB thật: ngày calories-only giữ kcal 2200, P/F/C null (không 0), KHÔNG Other = tổng kcal, không macro kcal giả; macro-only giữ protein, kcal/Other null; trung bình MỖI macro chỉ trên ngày có macro đó (protein 3 ngày, fat/carbs 2 ngày), kcal trên ngày có kcal; phần trăm null khi thiếu dữ liệu",
            ds[b.AddDays(1)] is { IsLogged: true, CaloriesKcal: 2200m, ProteinG: null, FatG: null, CarbsG: null, ProteinKcal: null, FatKcal: null, CarbsKcal: null, OtherKcal: null }
            && ds[b.AddDays(3)] is { IsLogged: true, CaloriesKcal: null, ProteinG: 90m, OtherKcal: null } && ds[b] is { CaloriesKcal: 2000m, OtherKcal: 60m } && ds[b.AddDays(2)] is { CaloriesKcal: 1800m, ProteinG: 20m, OtherKcal: 1470m }
            && ms.Value.Summary.LoggedDays == 4 && avg.CaloriesKcal == 2000m && Math.Abs(avg.ProteinG!.Value - 86.7m) <= 0.1m && avg.FatG == 35m && avg.CarbsG == 120m && avg.ProteinPercent is null && avg.FatPercent is null,
            $"calories-only: {ds[b.AddDays(1)].CaloriesKcal}/{ds[b.AddDays(1)].ProteinG?.ToString() ?? "null"}/other {ds[b.AddDays(1)].OtherKcal?.ToString() ?? "null"}; avg kcal {avg.CaloriesKcal}, protein {avg.ProteinG}, fat {avg.FatG}, carbs {avg.CarbsG}, protein% {avg.ProteinPercent?.ToString() ?? "null"}");
    }

    {
        // Đường VALIDATOR (isFasting + manual dương trong cùng request) đi qua ValidationPipelineBehavior THẬT + GlobalExceptionHandler THẬT: cũng phải có errors.reasonCode, và vẫn giữ lỗi theo trường
        var behavior = new ValidationPipelineBehavior<SaveNutritionDataCommand, NutritionDataDayModel>([new SaveNutritionDataCommandValidator()]);
        async Task<(int Status, string? Reason, string Body)> ViaPipeline(SaveNutritionDataCommand cmd)
        {
            try { await behavior.Handle(cmd, _ => Task.FromResult<NutritionDataDayModel>(null!), default); return (200, null, ""); }
            catch (Exception e) { return await Map(e); }
        }
        var d = today.AddDays(-1);
        var kcalFast = await ViaPipeline(new SaveNutritionDataCommand(d, 500, null, null, null, true, null));
        var macroFast = await ViaPipeline(new SaveNutritionDataCommand(d, null, 10m, null, null, true, null));
        var okFast = await ViaPipeline(new SaveNutritionDataCommand(d, 0, 0, 0, 0, true, null));          // nhịn ăn + 0 hợp lệ
        var outOfRange = await ViaPipeline(new SaveNutritionDataCommand(d, 10001, null, null, null, null, null));
        using var kj = JsonDocument.Parse(kcalFast.Body); var kr = kj.RootElement;
        using var oj = JsonDocument.Parse(outOfRange.Body);
        Check("22 Đường validator: PUT isFasting:true + calories:500 (hoặc chỉ protein dương) ⇒ 400 'Validation failed' có errors.reasonCode = FASTING_WITH_MANUAL_NUTRITION (cùng mã với đường handler) và VẪN giữ lỗi theo trường errors.IsFasting; nhịn ăn + 0 hợp lệ; lỗi validator khác (kcal 10001) KHÔNG bị gắn reasonCode giả",
            kcalFast is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && macroFast is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && kr.GetProperty("message").GetString() == "Validation failed"
            && kr.GetProperty("errors").TryGetProperty("IsFasting", out var fieldErr) && fieldErr.GetArrayLength() >= 1 && okFast.Status == 200
            && outOfRange.Status == 400 && !oj.RootElement.GetProperty("errors").TryGetProperty("reasonCode", out _),
            $"kcal+fasting {kcalFast.Status}:{kcalFast.Reason}, macro+fasting {macroFast.Status}:{macroFast.Reason}, fasting+0 {okFast.Status}, out of range {outOfRange.Status} reasonCode {(oj.RootElement.GetProperty("errors").TryGetProperty("reasonCode", out _) ? "present" : "absent")}");
    }

    // =========================== 10) ROUND 4: D2 fasting trong trung bình (parity mọi nơi), D3 ma trận nhịn ăn, DerivedDataSync Ready/Updating/Failed, chuỗi Food Log → NDM → Expenditure → Energy Balance ===========================
    using (var sc = NewCtx()) await NutrientData.SeedAsync(sc);   // định nghĩa chất (Overview, Nutrient Detail cần): dữ liệu tham chiếu, app seed lúc khởi động
    {
        // Cửa sổ 7 ngày: manual đủ 2000/150/60/200, nhịn ăn, calories-only 2200, vắng, món 1000 (P20 C40 F10), chỉ protein 90, hôm nay trống
        var u = Guid.NewGuid(); var t = today;
        await Seed(u, t.AddDays(-6), mk: 2000, mp: 150, mc: 200, mf: 60, note: false); await Seed(u, t.AddDays(-5), fasting: true, note: false); await Seed(u, t.AddDays(-4), mk: 2200, note: false);
        await Seed(u, t.AddDays(-2), foods: 2, kcalEach: 500, note: false); await Seed(u, t.AddDays(-1), mp: 90, note: false);
        // kỳ vọng: kcal ngày [2000, 0(nhịn ăn), 2200, 1000] = 1300; protein ngày [150, 0, 20, 90] = 65; fat [60, 0, 10] = 23.33; carbs [200, 0, 40] = 80; ngày vắng/ngày null không vào mẫu số
        var ms = (await Call(uow => new GetMacroSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System).Handle(new GetMacroSeriesQuery(t.AddDays(-6), t), default))).Value!.Summary.Averages;
        var ov = (await Call(uow => new GetNutritionOverviewQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetNutritionOverviewQuery(OverviewRange.Week, t), default))).Value!;
        decimal? OvV(string key) => ov.Items.First(i => i.Key == key).Value;
        async Task<decimal?> Detail(string code) => (await Call(uow => new GetNutrientSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System).Handle(new GetNutrientSeriesQuery(code, t.AddDays(-6), t), default))).Value!.Summary.Average;
        var eb = (await Call(uow => new GetEnergyBalanceSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetEnergyBalanceSeriesQuery(t.AddDays(-6), t), default))).Value!.Summary;
        bool Near(decimal? a, decimal e) => a is { } v && Math.Abs(v - e) <= 0.1m;
        var dKcal = await Detail("energy_kcal"); var dP = await Detail("protein_g"); var dF = await Detail("fat_g"); var dC = await Detail("carbs_g");
        Check("23 D2 parity: ngày Fasting hợp lệ = 0 kcal / 0 g P-F-C VÀO trung bình; ngày vắng và macro null (calories-only, protein-only) không vào mẫu số của macro đó. Macros, Overview (tuần), Nutrient Detail và Energy Balance cho CÙNG số: kcal 1300, protein 65, fat 23.3, carbs 80",
            Near(ms.CaloriesKcal, 1300m) && Near(ms.ProteinG, 65m) && Near(ms.FatG, 23.33m) && Near(ms.CarbsG, 80m)
            && Near(OvV("calories"), 1300m) && Near(OvV("protein"), 65m) && Near(OvV("fat"), 23.33m) && Near(OvV("carbs"), 80m)
            && Near(dKcal, 1300m) && Near(dP, 65m) && Near(dF, 23.33m) && Near(dC, 80m) && Near(eb.AvgCaloriesKcal, 1300m),
            $"macros {ms.CaloriesKcal}/{ms.ProteinG}/{ms.FatG}/{ms.CarbsG} | overview {OvV("calories")}/{OvV("protein")}/{OvV("fat")}/{OvV("carbs")} | detail {dKcal}/{dP}/{dF}/{dC} | energy balance kcal {eb.AvgCaloriesKcal}");
        var fastOnly = Guid.NewGuid(); await Seed(fastOnly, t.AddDays(-1), fasting: true, note: false);
        var ovF = (await Call(uow => new GetNutritionOverviewQueryHandler(Clock(uow, fastOnly), uow, new TestUser(fastOnly), TimeProvider.System).Handle(new GetNutritionOverviewQuery(OverviewRange.Week, t), default))).Value!;
        var msF = (await Call(uow => new GetMacroSeriesQueryHandler(uow, new TestUser(fastOnly), TimeProvider.System).Handle(new GetMacroSeriesQuery(t.AddDays(-6), t), default))).Value!;
        // (quan sát có sẵn từ trước D2: kỳ KHÔNG có món nào thì tổng vi chất của Overview là 0 chứ không null, vì NutrientCatalog.AmountsAsync cộng danh sách entry rỗng ra 0; không thuộc phạm vi D2, báo cáo riêng)
        Check("23b Chỉ có một ngày nhịn ăn trong kỳ: Overview và Macros đều báo 0 kcal / 0 g (đã log, không phải 'không có dữ liệu'), loggedDays = 1",
            ovF.Items.First(i => i.Key == "calories").Value == 0m && ovF.Items.First(i => i.Key == "protein").Value == 0m && ovF.LoggedDays == 1 && msF.Summary.Averages.CaloriesKcal == 0m && msF.Summary.Averages.ProteinG == 0m,
            $"overview kcal {ovF.Items.First(i => i.Key == "calories").Value}, protein {ovF.Items.First(i => i.Key == "protein").Value}, loggedDays {ovF.LoggedDays}, macros kcal {msF.Summary.Averages.CaloriesKcal}");
    }
    {
        // D2 mở rộng: tuần + THÁNG; ngày vắng / dòng rỗng KHÔNG BAO GIỜ là nhịn ăn; calories-only không bao giờ cho macro 0 giả (mọi consumer: Macros, Overview, Nutrient Detail, Energy Balance, thuật toán)
        var u = Guid.NewGuid(); var t = today;
        await Seed(u, t.AddDays(-6), mk: 2000, mp: 150, mc: 200, mf: 60, note: false); await Seed(u, t.AddDays(-5), fasting: true, note: false); await Seed(u, t.AddDays(-4), mk: 2200, note: false);
        await Seed(u, t.AddDays(-2), foods: 2, kcalEach: 500, note: false); await Seed(u, t.AddDays(-1), mp: 90, note: false);
        await Seed(u, t.AddDays(-3), note: false);        // dòng FoodLog RỖNG (không món, không manual, không nhịn ăn) = chưa log, KHÔNG phải nhịn ăn
        async Task<NutritionOverviewResponse> Ov(Guid who, OverviewRange r) => (await Call(uow => new GetNutritionOverviewQueryHandler(Clock(uow, who), uow, new TestUser(who), TimeProvider.System).Handle(new GetNutritionOverviewQuery(r, t), default))).Value!;
        decimal? V(NutritionOverviewResponse r, string key) => r.Items.First(i => i.Key == key).Value;
        var wk = await Ov(u, OverviewRange.Week); var mo = await Ov(u, OverviewRange.Month);
        var dd = await Days(u, t.AddDays(-6), t);
        bool Near(decimal? a, decimal e) => a is { } v && Math.Abs(v - e) <= 0.1m;
        Check("23c D2 tuần và THÁNG: Overview tháng (đọc aggregate) cho CÙNG trung bình macro với tuần (kcal 1300, protein 65, fat 23.33, carbs 80, fasting = 0 g, ngày vắng và dòng FoodLog rỗng không vào mẫu số); dòng rỗng và ngày vắng KHÔNG xuất hiện trong ngày đã log (không bị coi nhịn ăn), nhịn ăn hợp lệ có AlgorithmKcal 0",
            Near(V(wk, "calories"), 1300m) && Near(V(wk, "protein"), 65m) && Near(V(wk, "fat"), 23.33m) && Near(V(wk, "carbs"), 80m)
            && Near(V(mo, "calories"), 1300m) && Near(V(mo, "protein"), 65m) && Near(V(mo, "fat"), 23.33m) && Near(V(mo, "carbs"), 80m)
            && wk.LoggedDays == 5 && mo.LoggedDays == 5 && !dd.ContainsKey(t.AddDays(-3)) && !dd.ContainsKey(t.AddDays(-7)) && !dd.ContainsKey(t)
            && dd[t.AddDays(-5)] is { IsFasting: true, AlgorithmKcal: 0m, IsValidForAlgorithms: true } && dd[t.AddDays(-5)].Protein == 0m && dd[t.AddDays(-5)].Fat == 0m && dd[t.AddDays(-5)].Carbs == 0m,
            $"tuần {V(wk, "calories")}/{V(wk, "protein")}/{V(wk, "fat")}/{V(wk, "carbs")} loggedDays {wk.LoggedDays}; tháng {V(mo, "calories")}/{V(mo, "protein")}/{V(mo, "fat")}/{V(mo, "carbs")} loggedDays {mo.LoggedDays}; dòng rỗng trong ngày đã log {dd.ContainsKey(t.AddDays(-3))}; nhịn ăn P/F/C {dd[t.AddDays(-5)].Protein}/{dd[t.AddDays(-5)].Fat}/{dd[t.AddDays(-5)].Carbs}");
        // calories-only: không macro nào bị 0 giả ở mọi consumer
        var uk = Guid.NewGuid(); await Seed(uk, t.AddDays(-2), mk: 2200, note: false); await Seed(uk, t.AddDays(-1), mk: 1800, note: false);
        var wkK = await Ov(uk, OverviewRange.Week); var moK = await Ov(uk, OverviewRange.Month);
        var msK = (await Call(uow => new GetMacroSeriesQueryHandler(uow, new TestUser(uk), TimeProvider.System).Handle(new GetMacroSeriesQuery(t.AddDays(-6), t), default))).Value!.Summary.Averages;
        async Task<decimal?> DetailK(string code) => (await Call(uow => new GetNutrientSeriesQueryHandler(uow, new TestUser(uk), TimeProvider.System).Handle(new GetNutrientSeriesQuery(code, t.AddDays(-6), t), default))).Value!.Summary.Average;
        var ebK = (await Call(uow => new GetEnergyBalanceSeriesQueryHandler(uow, new TestUser(uk), TimeProvider.System, algo).Handle(new GetEnergyBalanceSeriesQuery(t.AddDays(-6), t), default))).Value!.Summary;
        var ddK = await Days(uk, t.AddDays(-6), t);
        Check("23d D2 calories-only (2200 + 1800, macro không nhập): Overview tuần/tháng, Macro summary và Nutrient Detail cho kcal 2000 và protein/fat/carbs = NULL (không 0 giả); Energy Balance kcal 2000; effective từng ngày macro null, ngày vẫn hợp lệ cho thuật toán",
            Near(V(wkK, "calories"), 2000m) && V(wkK, "protein") is null && V(wkK, "fat") is null && V(wkK, "carbs") is null
            && Near(V(moK, "calories"), 2000m) && V(moK, "protein") is null && V(moK, "fat") is null && V(moK, "carbs") is null
            && Near(msK.CaloriesKcal, 2000m) && msK.ProteinG is null && msK.FatG is null && msK.CarbsG is null
            && Near(await DetailK("energy_kcal"), 2000m) && await DetailK("protein_g") is null && await DetailK("fat_g") is null && await DetailK("carbs_g") is null
            && Near(ebK.AvgCaloriesKcal, 2000m) && ddK.Values.All(x => x.Protein is null && x.Fat is null && x.Carbs is null && x.IsValidForAlgorithms),
            $"overview tuần {V(wkK, "calories")}/{V(wkK, "protein")?.ToString() ?? "null"}, tháng {V(moK, "calories")}/{V(moK, "protein")?.ToString() ?? "null"}; macros {msK.CaloriesKcal}/{msK.ProteinG?.ToString() ?? "null"}; EB {ebK.AvgCaloriesKcal}");
    }
    {
        // D3: nhịn ăn ↔ manual dương (calories HAY bất kỳ macro) = 400 FASTING_WITH_MANUAL_NUTRITION; null / 0 tường minh KHÔNG xung đột; không xoá món, không bỏ nhịn ăn, không ghi đè manual
        var u = Guid.NewGuid(); var d = today.AddDays(-30); await SetFasting(u, d, true); var b0 = await Snapshot(u, d);
        var pos = new (string Name, Func<Task<(int Status, string? Reason, NutritionDataDayModel? Value)>> Run)[]
        {
            ("calories null + protein > 0", () => Save(u, d, p: 10)), ("calories null + carbs > 0", () => Save(u, d, c: 10)), ("calories null + fat > 0", () => Save(u, d, f: 10)),
            ("calories > 0", () => Save(u, d, 500)), ("calories > 0 + all macros", () => Save(u, d, 500, 10, 10, 10)),
        };
        var posBad = new List<string>();
        foreach (var (name, run) in pos) { var r = await run(); if (!(r is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" }) || await Snapshot(u, d) != b0) posBad.Add($"{name} → {r.Status}:{r.Reason}"); }
        var nullAll = await Save(u, d);   // tất cả null: không phải xung đột (chỉ là không có gì để lưu)
        var zeros = await Save(u, d, 0, 0, 0, 0); var zerosMacroOnly = await Save(u, d, p: 0, c: 0, f: 0); var zeroKcal = await Save(u, d, 0);
        var sAfter = await Snapshot(u, d);
        Check("24 D3 Save trên ngày nhịn ăn: calories null + P>0 / C>0 / F>0, calories>0 (+macro) đều 400 FASTING_WITH_MANUAL_NUTRITION và KHÔNG đổi gì (nhịn ăn còn, manual không ghi); tất cả null ⇒ 400 NOTHING_TO_SAVE (không phải xung đột); 0 tường minh (đủ bốn trường, chỉ macro, chỉ kcal) hợp lệ",
            posBad.Count == 0 && nullAll is { Status: 400, Reason: "NOTHING_TO_SAVE" } && zeros.Status == 200 && zerosMacroOnly.Status == 200 && zeroKcal.Status == 200 && sAfter.Fasting && sAfter.MCal == 0m,
            $"vi phạm [{string.Join("; ", posBad)}], null-all {nullAll.Status}:{nullAll.Reason}, zeros {zeros.Status}/{zerosMacroOnly.Status}/{zeroKcal.Status}, fasting {sAfter.Fasting}");
        // chiều ngược lại: bật nhịn ăn trên ngày đã có manual dương ở từng trường
        var rev = new List<string>();
        foreach (var (name, k, p, c, f) in new (string, decimal?, decimal?, decimal?, decimal?)[] { ("protein", null, 10, null, null), ("carbs", null, null, 10, null), ("fat", null, null, null, 10), ("calories", 500, null, null, null) })
        {
            var ur = Guid.NewGuid(); await Seed(ur, d, mk: k, mp: p, mc: c, mf: f, note: false); var br = await Snapshot(ur, d);
            var r = await SetFasting(ur, d, true); if (!(r is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" }) || await Snapshot(ur, d) != br) rev.Add($"{name} → {r.Status}:{r.Reason}");
        }
        var uz = Guid.NewGuid(); await Seed(uz, d, mk: 0, mp: 0, mc: 0, mf: 0, note: false); var fastOnZero = await SetFasting(uz, d, true);
        var uf = Guid.NewGuid(); await Seed(uf, d, foods: 2, kcalEach: 400, note: false); var bf = await Snapshot(uf, d); var fastOnFood = await SetFasting(uf, d, true); var saveFast = await Save(uf, d, fasting: true);
        Check("24b D3 bật nhịn ăn: trên manual dương ở calories/protein/carbs/fat ⇒ 400 FASTING_WITH_MANUAL_NUTRITION; trên manual 0 tường minh hợp lệ; trên món dương ⇒ 409 FASTING_NEEDS_EMPTY_DAY (cả qua PUT), không xoá món, manual và món nguyên vẹn",
            rev.Count == 0 && fastOnZero.Status == 200 && fastOnFood is { Status: 409, Reason: "FASTING_NEEDS_EMPTY_DAY" } && saveFast is { Status: 409, Reason: "FASTING_NEEDS_EMPTY_DAY" } && await Snapshot(uf, d) == bf,
            $"đảo chiều lỗi [{string.Join("; ", rev)}], on-zero {fastOnZero.Status}, on-food {fastOnFood.Status}:{fastOnFood.Reason}, via save {saveFast.Status}:{saveFast.Reason}");
        // đường validator cho từng trường dương (cùng reasonCode)
        var vb = new ValidationPipelineBehavior<SaveNutritionDataCommand, NutritionDataDayModel>([new SaveNutritionDataCommandValidator()]);
        var vBad = new List<string>();
        foreach (var (name, k, p, c, f) in new (string, decimal?, decimal?, decimal?, decimal?)[] { ("protein", null, 10, null, null), ("carbs", null, null, 10, null), ("fat", null, null, null, 10), ("calories", 500, null, null, null) })
        { try { await vb.Handle(new SaveNutritionDataCommand(d, k, p, c, f, true, null), _ => Task.FromResult<NutritionDataDayModel>(null!), default); vBad.Add(name + " passed"); } catch (Exception e) { var m = await Map(e); if (m is not { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" }) vBad.Add($"{name} → {m.Status}:{m.Reason}"); } }
        Check("24c D3 đường validator (isFasting:true kèm giá trị dương ở từng trường) cũng 400 + reasonCode FASTING_WITH_MANUAL_NUTRITION", vBad.Count == 0, $"lỗi [{string.Join("; ", vBad)}]");
    }
    {
        // DerivedDataSync: Ready → Updating (khoá user bị giữ ⇒ job bền) → Ready sau khi job chạy; Failed (job hết lượt) vẫn báo Failed sau thao tác manual
        var u = Guid.NewGuid(); var d = today.AddDays(-4); await SeedProgram(u, today.AddDays(-8));
        var opt = new AlgorithmOptions { DerivedLockWaitMs = 300 }; var optWrap = Options.Create(opt);
        async Task<string> State() { using var c = NewCtx(); return await DerivedDataJobs.OpenStateAsync(new UnitOfWork(c), u, opt, default); }
        var r1 = await Save(u, d, 2000, opt: optWrap); var s1 = await State();
        string sQueued; (int Status, string? Reason, NutritionDataDayModel? Value) r2; List<RecalculationJob> queued;
        using (var holderCtx = NewCtx())
        {
            var holder = new UnitOfWork(holderCtx); await holder.BeginTransactionAsync();
            if (!await holder.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(u), 1000)) throw new InvalidOperationException("holder could not lock");
            r2 = await Save(u, d, 2600, opt: optWrap);   // khoá bận quá 300 ms ⇒ nguồn đã lưu, dẫn xuất xếp job bền
            sQueued = await State();
            await holder.RollbackTransactionAsync();
        }
        using (var c = NewCtx()) queued = await c.Set<RecalculationJob>().Where(j => j.UserId == u && j.State == JobState.Pending).ToListAsync();
        var dpWhileQueued = await DailyProgress(u, d);
        using (var c = NewCtx()) await DerivedDataJobs.RunClaimedAsync(new UnitOfWork(c), u, queued[0].Id, opt, TimeProvider.System, default);
        var sReady = await State(); var dpAfter = await DailyProgress(u, d);
        var uF = Guid.NewGuid(); using (var c = NewCtx()) { c.Add(new RecalculationJob { Id = Guid.NewGuid(), UserId = uF, Cause = RecalculationCause.Manual, FromDate = d, ToDate = d, State = JobState.Failed, AttemptCount = opt.DerivedJobMaxAttempts }); await c.SaveChangesAsync(); }
        var rF = await Save(uF, today.AddDays(-3), 1800); string sFailed; using (var c = NewCtx()) sFailed = await DerivedDataJobs.OpenStateAsync(new UnitOfWork(c), uF, opt, default);
        Check("25 DerivedDataSync sau manual: Ready khi dựng xong; khoá user bị giữ ⇒ nguồn VẪN lưu (200), trạng thái Updating + job Pending; chạy job ⇒ Ready và DailyProgress theo số manual mới; job hết lượt ⇒ Failed (thao tác manual không che trạng thái Failed)",
            r1.Status == 200 && s1 == "Ready" && r2.Status == 200 && r2.Value?.Calories == 2600m && sQueued == "Updating" && queued.Count == 1 && dpWhileQueued.Consumed == 2000m && sReady == "Ready" && dpAfter is (2600m, true) && rF.Status == 200 && sFailed == "Failed",
            $"saved {r1.Status}/{s1} → locked {r2.Status}/{sQueued} (jobs {queued.Count}, DailyProgress còn {dpWhileQueued.Consumed}) → sau job {sReady}, DailyProgress {dpAfter} | job hết lượt {sFailed}");
    }
    {
        // Food Log → NDM → Dashboard (DailyProgress) → Expenditure (AlgorithmKcal) → Energy Balance: CÙNG một số ở mỗi bước
        var u = Guid.NewGuid(); var d = today.AddDays(-5); await SeedProgram(u, today.AddDays(-8));
        async Task<(decimal? Ndm, decimal? Algo, decimal? Eb, decimal? Dp, decimal? Raw, string? Src)> Probe()
        {
            var ndm = await Day(u, d); var algoKcal = (await Days(u, d, d)).GetValueOrDefault(d)?.AlgorithmKcal;
            var ebDay = (await Call(uow => new GetEnergyBalanceSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetEnergyBalanceSeriesQuery(d, d), default))).Value!.Days.Single();
            return (ndm?.Calories, algoKcal, ebDay.CaloriesKcal, (await DailyProgress(u, d)).Consumed, ndm?.FoodLogCalories, ndm?.Source);
        }
        var food = await AddFood(u, d, 1800); var p1 = await Probe();
        await Save(u, d, 2200); var p2 = await Probe();
        await AddFood(u, d, 300); var p3 = await Probe();
        await RemoveOverride(u, d); var p4 = await Probe();
        Check("26 Chuỗi Food Log → NDM → Dashboard → Expenditure → Energy Balance cùng một số mỗi bước: món 1800 → manual 2200 → thêm món vẫn 2200 (raw 2100) → gỡ override 2100; không mất món, không macro giả",
            food.Status == 200 && p1 is (1800m, 1800m, 1800m, 1800m, 1800m, "FoodLog") && p2 is (2200m, 2200m, 2200m, 2200m, 1800m, "Manual") && p3 is (2200m, 2200m, 2200m, 2200m, 2100m, "Manual") && p4 is (2100m, 2100m, 2100m, 2100m, 2100m, "FoodLog"),
            $"{p1} | {p2} | {p3} | {p4}");
    }
    {
        // POST /food-logs/plate/nutrients + /entries/nutrients: Day = foods + plate RAW (không áp override); EffectiveDay = Effective DAILY Nutrition (hiệu lực của NGÀY, không phải của plate/entry) (override thắng, null giữ null, FoodLog* = món + plate)
        var u = Guid.NewGuid(); var d1 = today.AddDays(-20); var d2 = today.AddDays(-21); var d3 = today.AddDays(-22); var d4 = today.AddDays(-23);
        Task<(int Status, string? Reason, PlateNutrientsResponse? Value)> Plate(DateOnly d, params PlateLine[] lines)
            => Call(uow => new GetPlateNutrientsQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetPlateNutrientsQuery(d, lines.ToList()), default));
        static decimal? Kcal(IReadOnlyList<NutrientAmountModel> rows) => rows.FirstOrDefault(r => r.Code == "energy_kcal")?.Amount;
        var estimate = new PlateLine(null, null, null, 1, "estimate", 100, 10, 5, 2);
        await AddFood(u, d1, 300); await Save(u, d1, 2000);                      // override 2000 + món 300 (+ plate 100)
        await AddFood(u, d2, 300);                                               // không override
        await Save(u, d3, 2000);                                                 // calories-only: macro null
        var o = await Plate(d1, estimate); var o2 = await Plate(d1, estimate); var n = await Plate(d2, estimate); var c = await Plate(d3, estimate); var empty = await Plate(d4);
        var eo = o.Value?.EffectiveDay; var eo2 = o2.Value?.EffectiveDay; var en = n.Value?.EffectiveDay; var ec = c.Value?.EffectiveDay;
        var entryIds = await LiveEntryIds(u, d1);
        Task<(int Status, string? Reason, PlateNutrientsResponse? Value)> Entries(Guid[] ids) => Call(uow => new GetEntriesNutrientsQueryHandler(uow, new TestUser(u)).Handle(new GetEntriesNutrientsQuery(ids.ToList()), default));
        static decimal? Row(IReadOnlyList<NutrientAmountModel> rows, string code) => rows.FirstOrDefault(r => r.Code == code)?.Amount;
        var viaEntries = await Entries(entryIds.ToArray());
        var dayAfterPreview = await Day(u, d1);   // xem trước chỉ đọc: ngày thật không đổi, plate không cộng đôi vào subtotal
        // macro-only override (protein 90, kcal null) và ngày nhịn ăn trống + plate / không plate
        var d5 = today.AddDays(-24); var d6 = today.AddDays(-25);
        await Save(u, d5, p: 90); await SetFasting(u, d6, true);
        var mo = (await Plate(d5, estimate)).Value?.EffectiveDay; var fastEmptyPlate = (await Plate(d6)).Value?.EffectiveDay; var fastWithPlate = (await Plate(d6, estimate)).Value?.EffectiveDay;
        var rm = await RemoveOverride(u, d1);     // gỡ override ⇒ EffectiveDay rơi về món + plate; Day (RAW) khớp, không cộng đôi
        var afterRm = await Plate(d1, estimate); var erm = afterRm.Value?.EffectiveDay; var entriesAfterRm = await Entries(entryIds.ToArray());
        Check("26b Plate/Entries nutrients: EffectiveDay = Effective DAILY Nutrition của ngày sau khi log plate (không phải của plate/entry). Dưới override 2000 + món 300 (P10 C20 F5) + plate 100 (P10 C5 F2): EffectiveDay.Calories 2000 Source Manual HasOverride, macro null (không lấy từ món, không 0), FoodLog* = 400/20/25/7 (cộng MỘT lần), Conflict null, IsValidForAlgorithms; Plate GIỮ giá trị gốc (100 kcal, P10), Day RAW = 400 (không áp override); gọi lặp không đổi và ngày thật không đổi (không cộng đôi)",
            o.Status == 200 && o.Value!.Date == d1 && eo is { Calories: 2000m, Source: "Manual", HasOverride: true, ProteinG: null, CarbsG: null, FatG: null, ManualCaloriesKcal: 2000m, ManualProteinG: null, FoodLogCalories: 400m, FoodLogProteinG: 20m, FoodLogCarbsG: 25m, FoodLogFatG: 7m, IsValidForAlgorithms: true, Conflict: null }
            && Row(o.Value.Plate, "energy_kcal") == 100m && Row(o.Value.Plate, "protein_g") == 10m && Row(o.Value.Day, "energy_kcal") == 400m && Row(o.Value.Day, "protein_g") == 20m
            && eo2 == eo && dayAfterPreview is { FoodLogCalories: 300m, Calories: 2000m },
            $"override: effective {eo?.Calories}/{eo?.ProteinG?.ToString() ?? "null"} {eo?.Source} foodlog {eo?.FoodLogCalories}/{eo?.FoodLogProteinG} | plate {Row(o.Value!.Plate, "energy_kcal")}/{Row(o.Value.Plate, "protein_g")} | day {Row(o.Value.Day, "energy_kcal")}/{Row(o.Value.Day, "protein_g")} | repeat same {eo2 == eo} | real day foodlog {dayAfterPreview?.FoodLogCalories}");
        Check("26c Không override ⇒ EffectiveDay = món + plate, Source FoodLog (Calories 400 = Day RAW 400, không cộng đôi); calories-only không món ⇒ macro null (plate không lấp vào), FoodLog* chỉ plate (100/P10); macro-only (protein 90) ⇒ Calories null, ProteinG 90; ngày vắng + plate rỗng ⇒ EffectiveDay null; nhịn ăn trống: không plate ⇒ Source Fasting 0 kcal, có plate ⇒ bỏ nhịn ăn (FoodLog 100)",
            en is { Calories: 400m, Source: "FoodLog", HasOverride: false, ProteinG: 20m, FoodLogCalories: 400m } && Row(n.Value!.Day, "energy_kcal") == 400m && en.Calories == Row(n.Value.Day, "energy_kcal")
            && ec is { Calories: 2000m, ProteinG: null, CarbsG: null, FatG: null, FoodLogCalories: 100m, FoodLogProteinG: 10m }
            && mo is { Calories: null, ProteinG: 90m, CarbsG: null, FatG: null, Source: "Manual", FoodLogCalories: 100m }
            && empty.Status == 200 && empty.Value!.EffectiveDay is null
            && fastEmptyPlate is { Source: "Fasting", Calories: 0m } && fastWithPlate is { Source: "FoodLog", Calories: 100m, FoodLogCalories: 100m },
            $"no-override {en?.Calories}/{en?.ProteinG} {en?.Source} | kcal-only {ec?.Calories}/{ec?.ProteinG?.ToString() ?? "null"} foodlog {ec?.FoodLogCalories} | macro-only {mo?.Calories?.ToString() ?? "null"}/{mo?.ProteinG} | empty {(empty.Value!.EffectiveDay is null ? "null" : "set")} | fasting {fastEmptyPlate?.Source}/{fastEmptyPlate?.Calories} → {fastWithPlate?.Source}/{fastWithPlate?.Calories}");
        Check("26d POST entries/nutrients (chọn entry): Plate = tổng RAW entry chọn (300 kcal) giữ giá trị gốc dưới override; EffectiveDay vẫn là NGÀY (2000 Manual, FoodLog 300), không phải 300; Remove Override ⇒ EffectiveDay rơi về món (400 với plate / 300 entries) Source FoodLog, Day RAW khớp, không cộng đôi",
            viaEntries.Status == 200 && Row(viaEntries.Value!.Plate, "energy_kcal") == 300m && viaEntries.Value.EffectiveDay is { Calories: 2000m, Source: "Manual", FoodLogCalories: 300m, ProteinG: null }
            && rm.Status == 200 && erm is { Calories: 400m, Source: "FoodLog", HasOverride: false, FoodLogCalories: 400m, ProteinG: 20m } && Row(afterRm.Value!.Day, "energy_kcal") == erm.Calories
            && entriesAfterRm.Value!.EffectiveDay is { Calories: 300m, Source: "FoodLog", HasOverride: false } && Row(entriesAfterRm.Value.Plate, "energy_kcal") == 300m,
            $"entries under override: plate {Row(viaEntries.Value!.Plate, "energy_kcal")} effectiveDay {viaEntries.Value.EffectiveDay?.Calories} {viaEntries.Value.EffectiveDay?.Source} foodlog {viaEntries.Value.EffectiveDay?.FoodLogCalories} | after remove: plate-preview effectiveDay {erm?.Calories} {erm?.Source}, day {Row(afterRm.Value!.Day, "energy_kcal")}; entries effectiveDay {entriesAfterRm.Value?.EffectiveDay?.Calories} {entriesAfterRm.Value?.EffectiveDay?.Source}");
    }

    {
        // D3 các đường ghi còn lại: Quick Edit trên ngày nhịn ăn; PUT isFasting:true trên ngày đã có manual dương (calories null) / manual 0 / có món; không tự xoá món hay manual
        var u = Guid.NewGuid(); var d = today.AddDays(-40); await SetFasting(u, d, true); var b0 = await Snapshot(u, d);
        var qe = await QuickEdit(u, d, 500, 10, 10, 10); var qeZero = await QuickEdit(u, d, 0, 0, 0, 0); var qeKcalOnly = await QuickEdit(u, d, 300);
        var afterQe = await Snapshot(u, d);
        var um = Guid.NewGuid(); await Seed(um, d, mp: 40, note: false); var bm = await Snapshot(um, d);
        var putOnManual = await Save(um, d, fasting: true); var afterPut = await Snapshot(um, d);
        // gửi RÕ protein 0 cùng isFasting:true là chính user đặt giá trị cuối không xung đột (nhịn ăn + manual 0): hợp lệ, KHÔNG phải BE tự ghi đè
        var um0 = Guid.NewGuid(); await Seed(um0, d, mp: 40, note: false); var putOnManualWithZero = await Save(um0, d, p: 0, fasting: true); var afterPut0 = await Snapshot(um0, d);
        var uz = Guid.NewGuid(); await Seed(uz, d, mk: 0, mp: 0, mc: 0, mf: 0, note: false); var putOnZero = await Save(uz, d, fasting: true);
        var uf = Guid.NewGuid(); await Seed(uf, d, foods: 1, kcalEach: 300, note: false); var bf = await Snapshot(uf, d); var putOnFoods = await Save(uf, d, p: 0, fasting: true); var afterFoods = await Snapshot(uf, d);
        Check("24d D3 các đường còn lại: Quick Edit trên ngày nhịn ăn với kcal/macro dương ⇒ 400 FASTING_WITH_MANUAL_NUTRITION và không đổi gì (kcal-only dương cũng vậy), nhưng toàn 0 hợp lệ; PUT isFasting:true trên ngày đã có protein manual > 0 (calories null) ⇒ 400 FASTING_WITH_MANUAL_NUTRITION, KHÔNG xoá/ghi đè manual (protein vẫn 40); gửi RÕ protein 0 cùng isFasting:true là user tự đặt trạng thái không xung đột ⇒ hợp lệ; trên manual 0 hợp lệ; trên ngày có món ⇒ 409 FASTING_NEEDS_EMPTY_DAY, món còn nguyên",
            qe is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && qeKcalOnly is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && qeZero.Status == 200 && afterQe.Fasting
            && putOnManual is { Status: 400, Reason: "FASTING_WITH_MANUAL_NUTRITION" } && afterPut == bm && afterPut.MP == 40m
            && putOnManualWithZero.Status == 200 && afterPut0 is { Fasting: true, MP: 0m }
            && putOnZero.Status == 200 && putOnFoods is { Status: 409, Reason: "FASTING_NEEDS_EMPTY_DAY" } && afterFoods == bf,
            $"quickedit dương {qe.Status}:{qe.Reason}, kcal-only {qeKcalOnly.Status}:{qeKcalOnly.Reason}, toàn 0 {qeZero.Status}, fasting còn {afterQe.Fasting} | PUT fasting trên protein manual {putOnManual.Status}:{putOnManual.Reason}, manual còn {afterPut.MP}; kèm protein 0 tường minh {putOnManualWithZero.Status} (manual {afterPut0.MP}, fasting {afterPut0.Fasting}) | trên 0 {putOnZero.Status} | trên món {putOnFoods.Status}:{putOnFoods.Reason}, món nguyên vẹn {afterFoods == bf}");
    }

    {
        // F6 (FE E2E): Nutrient Detail "Today" (amount + contributors + timing), GET /nutrients?date=, Overview Today contributors và GET expenditure/daily phải theo Effective Daily Nutrition
        var d = today.AddDays(-3);
        async Task<Guid> User() { var x = Guid.NewGuid(); await SeedProgram(x, today.AddDays(-12)); return x; }
        Task<(int Status, string? Reason, NutrientDayDetailResponse? Value)> Detail(Guid u, string code)
            => Call(uow => new GetNutrientDayDetailQueryHandler(uow, new TestUser(u), TimeProvider.System).Handle(new GetNutrientDayDetailQuery(code, d), default));
        async Task<Dictionary<string, decimal?>> DayAmounts(Guid u) => (await Call(uow => new GetDayNutrientsQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetDayNutrientsQuery(d), default))).Value!.Items.ToDictionary(i => i.Code, i => i.Amount);
        async Task<NutritionContributors?> TodayContributors(Guid u) => (await Call(uow => new GetNutritionOverviewQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetNutritionOverviewQuery(OverviewRange.Today, d), default))).Value!.Contributors;
        async Task<DailyProgressModel?> Daily(Guid u, DateOnly on) => (await Call(uow => new GetDailyProgressQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetDailyProgressQuery(on, on), default))).Value!.FirstOrDefault();
        static bool NoContributors(NutritionContributors? c) => c is { Calories.Count: 0, Protein.Count: 0, Fat.Count: 0, Carbs.Count: 0 };

        // calories-only override 2000 + món 405 kcal (P10 C20 F5)
        var ua = await User(); await AddFood(ua, d, 405); await Save(ua, d, 2000);
        var aE = (await Detail(ua, "energy_kcal")).Value!; var aP = (await Detail(ua, "protein_g")).Value!; var aC = (await Detail(ua, "carbs_g")).Value!; var aF = (await Detail(ua, "fat_g")).Value!; var aN = (await Detail(ua, "carbs_net_g")).Value!;
        Check("F6a Nutrient Detail Today dưới override calories-only 2000 + món 405: Amount energy = 2000 (không 405), protein/carbs/fat/net carbs = NULL (không lấy từ món, không 0), AmountSource Manual, Contributors RỖNG và Timing toàn 0 cho cả 5 mã (món không được tính)",
            aE is { Amount: 2000m, AmountSource: "Manual" } && aP is { Amount: null, AmountSource: "Manual" } && aC.Amount is null && aF.Amount is null && aN.Amount is null
            && new[] { aE, aP, aC, aF, aN }.All(x => x.Contributors.Count == 0 && x.Timing.Count == 24 && x.Timing.All(t => t.Amount == 0)),
            $"energy {aE.Amount}/{aE.AmountSource} contributors {aE.Contributors.Count}; protein {aP.Amount?.ToString() ?? "null"}/{aP.AmountSource} contributors {aP.Contributors.Count}; carbs {aC.Amount?.ToString() ?? "null"}; fat {aF.Amount?.ToString() ?? "null"}; net {aN.Amount?.ToString() ?? "null"}");
        var amA = await DayAmounts(ua);
        Check("F6b GET /nutrients?date= (cùng nguồn lượng) dưới override calories-only: energy 2000, protein/carbs/fat/net carbs null; Overview Today Top-3 Contributors RỖNG (cả bốn macro) vì món không được tính",
            amA["energy_kcal"] == 2000m && amA["protein_g"] is null && amA["carbs_g"] is null && amA["fat_g"] is null && amA["carbs_net_g"] is null && NoContributors(await TodayContributors(ua)),
            $"energy {amA["energy_kcal"]}, protein {amA["protein_g"]?.ToString() ?? "null"}, net {amA["carbs_net_g"]?.ToString() ?? "null"}");
        var dpA = await Daily(ua, d);
        Check("F6c GET expenditure/daily dưới override calories-only: IsLogged true, CaloriesConsumed 2000, Protein/Carbs/Fat Consumed = NULL (không 0 giả)",
            dpA is { IsLogged: true, CaloriesConsumed: 2000m, ProteinConsumed: null, CarbsConsumed: null, FatConsumed: null }, $"{dpA?.CaloriesConsumed}/{dpA?.ProteinConsumed?.ToString() ?? "null"}/{dpA?.CarbsConsumed?.ToString() ?? "null"}/{dpA?.FatConsumed?.ToString() ?? "null"} logged {dpA?.IsLogged}");

        // override đủ bốn trường
        var ub = await User(); await AddFood(ub, d, 405); await Save(ub, d, 2000, 150, 60, 200);
        var bP = (await Detail(ub, "protein_g")).Value!; var bC = (await Detail(ub, "carbs_g")).Value!; var bF = (await Detail(ub, "fat_g")).Value!; var bE = (await Detail(ub, "energy_kcal")).Value!; var dpB = await Daily(ub, d);
        Check("F6d Override đủ: Nutrient Detail Amount = manual (kcal 2000, P150, C200, F60), Contributors rỗng, AmountSource Manual; expenditure/daily macro = 150/200/60",
            bE.Amount == 2000m && bP.Amount == 150m && bC.Amount == 200m && bF.Amount == 60m && bP.AmountSource == "Manual" && bP.Contributors.Count == 0 && dpB is { ProteinConsumed: 150m, CarbsConsumed: 200m, FatConsumed: 60m },
            $"detail {bE.Amount}/{bP.Amount}/{bC.Amount}/{bF.Amount}; daily {dpB?.ProteinConsumed}/{dpB?.CarbsConsumed}/{dpB?.FatConsumed}");

        // không override: hành vi cũ giữ nguyên (món là nguồn, có đóng góp)
        var uc = await User(); await AddFood(uc, d, 405);
        var cE = (await Detail(uc, "energy_kcal")).Value!; var cP = (await Detail(uc, "protein_g")).Value!; var amC = await DayAmounts(uc); var dpC = await Daily(uc, d); var coC = await TodayContributors(uc);
        Check("F6e Không override: Nutrient Detail Amount 405 / protein 10, AmountSource FoodLog, Contributors 1 món, Timing có giá trị; /nutrients?date= 405; Overview Today có contributor; expenditure/daily macro 10/20/5",
            cE is { Amount: 405m, AmountSource: "FoodLog" } && cE.Contributors.Count == 1 && cE.Timing.Sum(t => t.Amount) == 405m && cP.Amount == 10m && amC["energy_kcal"] == 405m && !NoContributors(coC) && dpC is { ProteinConsumed: 10m, CarbsConsumed: 20m, FatConsumed: 5m },
            $"energy {cE.Amount}/{cE.AmountSource} contributors {cE.Contributors.Count} timing {cE.Timing.Sum(t => t.Amount)}; protein {cP.Amount}; daily {dpC?.ProteinConsumed}/{dpC?.CarbsConsumed}/{dpC?.FatConsumed}");

        // gỡ override: về nguồn Food Log (không còn null/Manual)
        await RemoveOverride(ua, d);
        var rE = (await Detail(ua, "energy_kcal")).Value!; var rP = (await Detail(ua, "protein_g")).Value!; var dpR = await Daily(ua, d); var coR = await TodayContributors(ua);
        Check("F6f Remove Override: Nutrient Detail trở lại món (energy 405, protein 10, FoodLog, 1 contributor), Overview Today có contributor, expenditure/daily macro 10/20/5",
            rE is { Amount: 405m, AmountSource: "FoodLog" } && rE.Contributors.Count == 1 && rP.Amount == 10m && !NoContributors(coR) && dpR is { ProteinConsumed: 10m, CarbsConsumed: 20m, FatConsumed: 5m },
            $"energy {rE.Amount}/{rE.AmountSource}, protein {rP.Amount}, daily {dpR?.ProteinConsumed}/{dpR?.CarbsConsumed}/{dpR?.FatConsumed}");

        // nhịn ăn hợp lệ = 0 g đã log; macro-only: kcal chưa biết (IsLogged false) nhưng protein 90 biết, carbs/fat null; ngày không log: macro null
        var ud = await User(); await SetFasting(ud, d, true);
        var fE = (await Detail(ud, "energy_kcal")).Value!; var fP = (await Detail(ud, "protein_g")).Value!; var dpF = await Daily(ud, d);
        var ue = await User(); await Save(ue, d, p: 90); var dpE = await Daily(ue, d);
        var dpMissing = await Daily(ue, d.AddDays(1));
        Check("F6g Nhịn ăn hợp lệ: Amount 0, AmountSource Fasting, daily macro 0/0/0 IsLogged true; macro-only (protein 90): daily Protein 90, Carbs/Fat null, IsLogged false; ngày không có log: macro null (nếu có dòng snapshot)",
            fE is { Amount: 0m, AmountSource: "Fasting" } && fP.Amount == 0m && dpF is { IsLogged: true, ProteinConsumed: 0m, CarbsConsumed: 0m, FatConsumed: 0m }
            && dpE is { IsLogged: false, ProteinConsumed: 90m, CarbsConsumed: null, FatConsumed: null } && (dpMissing is null || dpMissing is { IsLogged: false, ProteinConsumed: null, CarbsConsumed: null, FatConsumed: null }),
            $"fasting {fE.Amount}/{fE.AmountSource}, daily {dpF?.ProteinConsumed}/{dpF?.CarbsConsumed}/{dpF?.FatConsumed}; macro-only {dpE?.ProteinConsumed}/{dpE?.CarbsConsumed?.ToString() ?? "null"} logged {dpE?.IsLogged}; missing {(dpMissing is null ? "no row" : dpMissing.ProteinConsumed?.ToString() ?? "null")}");
    }

    // =========================== 11) D4 (CHỈ LocalDB): diễn tập sản xuất trên BẢN SAO đại diện bằng ĐÚNG script SQL đã sinh ===========================
    {
        string CsFor(string name) => $"{server}Database={name};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";
        async Task<T?> ScalarOn<T>(string conn, string sql)
        {
            await using var c = new Microsoft.Data.SqlClient.SqlConnection(conn); await c.OpenAsync();
            await using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.CommandTimeout = 120;
            var v = await cmd.ExecuteScalarAsync();
            return v is null or DBNull ? default : (T)Convert.ChangeType(v, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }
        async Task ExecOn(string conn, string sql)
        {
            await using var c = new Microsoft.Data.SqlClient.SqlConnection(conn); await c.OpenAsync();
            await using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.CommandTimeout = 180; await cmd.ExecuteNonQueryAsync();
        }
        async Task DropDb(string name)
        {
            await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True"); await m.OpenAsync();
            await using var cmd = m.CreateCommand(); cmd.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END"; await cmd.ExecuteNonQueryAsync();
        }
        // Mỗi script chạy trên MỘT kết nối riêng, tách lô theo dòng GO (như sqlcmd); lỗi thì ném, kết nối đóng ⇒ transaction dang dở của script bị rollback
        async Task RunScript(string conn, string text)
        {
            foreach (var batch in System.Text.RegularExpressions.Regex.Split(text, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) await ExecOn(conn, batch);
        }
        string? FindScript(string file)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "Docs", "Design", "Ops", file))) return Path.Combine(dir.FullName, "Docs", "Design", "Ops", file);
            return null;
        }
        var upPath = FindScript("S29_ManualNutrition.up.idempotent.sql"); var downPath = FindScript("S29_ManualNutrition.down.idempotent.sql");
        if (upPath is null || downPath is null) { Check("27 Script SQL S29 tìm thấy trong Docs/Design/Ops", false, "thiếu tệp .sql đã sinh"); }
        else
        {
            var upSql = File.ReadAllText(upPath); var downSql = File.ReadAllText(downPath);
            var dbCopy = "MacroOS_NdmCopy"; var dbOld = "MacroOS_NdmCopyS28"; var dbRestored = "MacroOS_NdmCopyRestored";
            var csCopy = CsFor(dbCopy); var csOld = CsFor(dbOld); var csRestored = CsFor(dbRestored);
            var optsCopy = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(csCopy).AddInterceptors(audit).Options;
            var optsOld = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(csOld).AddInterceptors(audit).Options;
            var bak = Path.Combine(Path.GetTempPath(), "ndm_s28_pre_s29.bak");
            foreach (var n in new[] { dbCopy, dbOld, dbRestored }) await DropDb(n);
            if (File.Exists(bak)) File.Delete(bak);
            try
            {
                // bản sao ở S28 với dữ liệu đại diện: 3 user × 60 ngày (món + nước + ghi chú; Quick Add cũ riêng và lẫn món; nhịn ăn; cờ chưa đủ; ngày chỉ có dòng) + vài entry xoá mềm
                using (var c = new ApplicationDbContext(optsCopy)) { await c.GetService<IMigrator>().MigrateAsync(s28); }
                await ExecOn(csCopy, "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
                Func<ApplicationDbContext> legacyCopy = () => new LegacyContext(optsCopy);
                var seedStart = today.AddDays(-80);
                foreach (var _ in Enumerable.Range(0, 3))
                {
                    var uu = Guid.NewGuid();
                    for (var i = 0; i < 60; i++)
                    {
                        var dd = seedStart.AddDays(i);
                        switch (i % 6)
                        {
                            case 0: await Seed(uu, dd, foods: 3, kcalEach: 500, waterMl: 300, note: true, make: legacyCopy); break;
                            case 1: await Seed(uu, dd, legacyQuickAdd: 400, note: false, make: legacyCopy); break;
                            case 2: await Seed(uu, dd, fasting: true, waterMl: 150, make: legacyCopy); break;
                            case 3: await Seed(uu, dd, foods: 2, kcalEach: 700, incomplete: true, make: legacyCopy); break;
                            case 4: await Seed(uu, dd, foods: 2, kcalEach: 300, legacyQuickAdd: 250, make: legacyCopy); break;
                            default: await Seed(uu, dd, note: true, make: legacyCopy); break;
                        }
                    }
                }
                await ExecOn(csCopy, "UPDATE TOP (25) FoodLogEntries SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME() WHERE Source = 'Search'");
                string FpOn() => Fp();
                var pre = (await ScalarOn<string>(csCopy, FpOn()))!;
                var preCounts = await ScalarOn<string>(csCopy, "SELECT CONCAT((SELECT COUNT(*) FROM FoodLogs WHERE IsFasting=1),'|',(SELECT COUNT(*) FROM FoodLogs WHERE IsIncomplete=1),'|',(SELECT COUNT(*) FROM FoodLogEntries WHERE Source='QuickAdd'),'|',(SELECT COUNT(*) FROM FoodLogEntries WHERE IsDeleted=1),'|',(SELECT COUNT(*) FROM FoodLogs WHERE Notes IS NOT NULL))");
                await ExecOn($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True", $"BACKUP DATABASE [{dbCopy}] TO DISK = N'{bak}' WITH INIT, FORMAT");   // sao lưu TRƯỚC khi áp S29
                // áp ĐÚNG script idempotent đã sinh
                await RunScript(csCopy, upSql);
                await ExecOn(csCopy, "ALTER TABLE FoodLogs WITH CHECK CHECK CONSTRAINT CK_FoodLogs_ManualNutrition_Range");
                var post = (await ScalarOn<string>(csCopy, FpOn()))!;
                var postCounts = await ScalarOn<string>(csCopy, "SELECT CONCAT((SELECT COUNT(*) FROM FoodLogs WHERE IsFasting=1),'|',(SELECT COUNT(*) FROM FoodLogs WHERE IsIncomplete=1),'|',(SELECT COUNT(*) FROM FoodLogEntries WHERE Source='QuickAdd'),'|',(SELECT COUNT(*) FROM FoodLogEntries WHERE IsDeleted=1),'|',(SELECT COUNT(*) FROM FoodLogs WHERE Notes IS NOT NULL))");
                var cols = await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%' AND IS_NULLABLE='YES' AND DATA_TYPE='decimal' AND NUMERIC_PRECISION=8 AND NUMERIC_SCALE=2 AND COLUMN_DEFAULT IS NULL");
                var manualRows = await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
                var histS29 = await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = N'20261025103001_S29_ManualNutrition'");
                Check("27 Bản sao đại diện (180 ngày: món, Quick Add cũ riêng/lẫn món, nhịn ăn, chưa đủ, nước, ghi chú, entry xoá mềm): áp script SQL đã sinh ⇒ FoodLogs/FoodLogEntries/nước/ghi chú/Fasting/Incomplete/Quick Add cũ KHÔNG đổi (checksum + số đếm trước/sau giống hệt), 4 cột decimal(8,2) NULL không default, 0 dòng manual, lịch sử có S29",
                    pre == post && preCounts == postCounts && cols == 4 && manualRows == 0 && histS29 == 1, $"fingerprint trước {pre} / sau {post}; đếm (fasting|incomplete|quickAdd|xoá mềm|ghi chú) {preCounts} → {postCounts}; cột đúng {cols}; manual {manualRows}; lịch sử S29 {histS29}");
                await RunScript(csCopy, upSql);   // lần hai: idempotent
                Check("27b Script idempotent: chạy lần hai không lỗi, không đổi dữ liệu, lịch sử S29 vẫn đúng 1 dòng", (await ScalarOn<string>(csCopy, FpOn())) == pre && await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = N'20261025103001_S29_ManualNutrition'") == 1, "chạy lần hai OK");

                // tương thích: BE CŨ (mô hình không có 4 cột) trên schema MỚI vẫn đọc/ghi được, không đụng cột manual
                int oldBeRead; bool oldBeWrote; int oldBeManual;
                using (var oc = new LegacyContext(optsCopy)) oldBeRead = await oc.Set<FoodLog>().CountAsync();
                var ou = Guid.NewGuid(); await Seed(ou, today.AddDays(-2), foods: 1, kcalEach: 300, make: legacyCopy); oldBeWrote = true;
                oldBeManual = await ScalarOn<int>(csCopy, $"SELECT COUNT(*) FROM FoodLogs WHERE UserId = '{ou}' AND (ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL)");
                // BE MỚI trên schema CŨ (S28) KHÔNG chạy được: thiếu cột ⇒ phải migrate TRƯỚC khi chạy BE mới (khởi động tự migrate chính là bước này)
                using (var oldc = new ApplicationDbContext(optsOld)) await oldc.GetService<IMigrator>().MigrateAsync(s28);
                string? newOnOld = null;
                try { using var nc = new ApplicationDbContext(optsOld); await new GetNutritionDataQueryHandler(new UnitOfWork(nc), new TestUser(ou)).Handle(new GetNutritionDataQuery(null, null), default); } catch (Exception e) { newOnOld = (e.InnerException ?? e).Message; }
                Check("27c Tương thích: BE CŨ + schema MỚI chạy bình thường (đọc 180+ dòng, ghi ngày mới, cột manual vẫn NULL); BE MỚI + schema CŨ (S28) lỗi 'Invalid column name' (ManualCaloriesKcal) ⇒ BẮT BUỘC migrate trước khi BE mới chạy",
                    oldBeRead >= 180 && oldBeWrote && oldBeManual == 0 && newOnOld is not null && newOnOld.Contains("Invalid column name"), $"BE cũ đọc {oldBeRead} dòng, ghi {oldBeWrote}, manual {oldBeManual}; BE mới trên S28: [{(newOnOld is null ? "chạy được (không mong đợi)" : newOnOld[..Math.Min(60, newOnOld.Length)])}]");

                // 27f Công tắc Database:AutoMigrate (MigrationStartup, mặc định true = như cũ): false + còn S29 chưa áp ⇒ DỪNG khởi động, KHÔNG áp gì (lịch sử vẫn ở S28, không có cột manual); true ⇒ áp S29 như hành vi cũ
                string? stopMsg = null;
                try { using var fc = new ApplicationDbContext(optsOld); await MigrationStartup.ApplyAsync(fc, false, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance); } catch (InvalidOperationException e) { stopMsg = e.Message; }
                var lastAfterOff = await ScalarOn<string>(csOld, "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC");
                var colsAfterOff = await ScalarOn<int>(csOld, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
                using (var onc = new ApplicationDbContext(optsOld)) await MigrationStartup.ApplyAsync(onc, true, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
                var lastAfterOn = await ScalarOn<string>(csOld, "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC");
                using (var okc = new ApplicationDbContext(optsOld)) await MigrationStartup.ApplyAsync(okc, false, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);   // schema đã đủ + false ⇒ chạy tiếp, không ném
                Check("27f Database:AutoMigrate=false + S29 chưa áp ⇒ khởi động DỪNG với thông báo (lịch sử vẫn S28, 0 cột Manual); =true (mặc định) áp S29; false khi schema đã đủ ⇒ chạy tiếp",
                    stopMsg is not null && stopMsg.Contains("S29_ManualNutrition") && lastAfterOff == s28 && colsAfterOff == 0 && lastAfterOn == "20261025103001_S29_ManualNutrition", $"stop [{stopMsg?[..Math.Min(70, stopMsg.Length)]}], sau false: {lastAfterOff}, cột {colsAfterOff}, sau true: {lastAfterOn}");

                // khôi phục từ bản sao lưu TRƯỚC S29: giống hệt trạng thái trước (S28), rồi diễn tập lại áp S29 trên bản khôi phục
                string RestoreSql()
                {
                    return $"RESTORE DATABASE [{dbRestored}] FROM DISK = N'{bak}' WITH REPLACE";
                }
                var files = new List<(string Logical, string Type)>();
                await using (var mc = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True"))
                {
                    await mc.OpenAsync(); await using var fc = mc.CreateCommand(); fc.CommandText = $"RESTORE FILELISTONLY FROM DISK = N'{bak}'";
                    await using var rd = await fc.ExecuteReaderAsync(); while (await rd.ReadAsync()) files.Add((rd.GetString(0), rd.GetString(2)));
                }
                var moves = string.Join(", ", files.Select(f => $"MOVE N'{f.Logical}' TO N'{Path.Combine(Path.GetTempPath(), dbRestored + (f.Type == "L" ? ".ldf" : f.Logical.EndsWith("_log") ? ".ldf" : ".mdf"))}'"));
                await ExecOn($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True", $"RESTORE DATABASE [{dbRestored}] FROM DISK = N'{bak}' WITH REPLACE, {moves}");
                var restoredFp = await ScalarOn<string>(csRestored, FpOn());
                var restoredCols = await ScalarOn<int>(csRestored, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
                var restoredLast = await ScalarOn<string>(csRestored, "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC");
                await RunScript(csRestored, upSql);
                var restoredPost = await ScalarOn<string>(csRestored, FpOn());
                Check("27d Khôi phục từ bản sao lưu TRƯỚC S29 (BACKUP/RESTORE trên LocalDB): dữ liệu giống hệt trạng thái trước (checksum), không có cột manual, lịch sử dừng ở S28; áp lại script trên bản khôi phục cho kết quả giống lần đầu (diễn tập lặp lại được)",
                    restoredFp == pre && restoredCols == 0 && restoredLast!.EndsWith("S28_GoalTrackingMode") && restoredPost == pre, $"khôi phục == trước {restoredFp == pre}, cột {restoredCols}, migration cuối {restoredLast}, áp lại == trước {restoredPost == pre}");

                // rollback bằng script Down: TỪ CHỐI khi còn manual (kết nối đóng ⇒ rollback, cột + dữ liệu còn), cho phép sau khi sao lưu + chủ động xoá giá trị
                await ExecOn(csCopy, "UPDATE TOP (2) FoodLogs SET ManualCaloriesKcal = 2200, ManualProteinG = NULL, ManualCarbsG = 250, ManualFatG = 0");
                var manualBeforeDown = await ScalarOn<string>(csCopy, "SELECT CONCAT(COUNT(*),'|',SUM(ManualCaloriesKcal),'|',SUM(ManualCarbsG)) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL");
                int? downErr = null;
                try { await RunScript(csCopy, downSql); } catch (Microsoft.Data.SqlClient.SqlException e) { downErr = e.Number; }
                var colsAfterRefusedDown = await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
                var manualAfterRefusedDown = await ScalarOn<string>(csCopy, "SELECT CONCAT(COUNT(*),'|',SUM(ManualCaloriesKcal),'|',SUM(ManualCarbsG)) FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL");
                await ExecOn(csCopy, "SELECT Id, UserId, LogDate, ManualCaloriesKcal, ManualProteinG, ManualCarbsG, ManualFatG INTO dbo.FoodLogManualBackup FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR ManualProteinG IS NOT NULL OR ManualCarbsG IS NOT NULL OR ManualFatG IS NOT NULL");
                await ExecOn(csCopy, "UPDATE FoodLogs SET ManualCaloriesKcal = NULL, ManualProteinG = NULL, ManualCarbsG = NULL, ManualFatG = NULL");
                await RunScript(csCopy, downSql);
                var colsAfterDown2 = await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FoodLogs' AND COLUMN_NAME LIKE 'Manual%'");
                var backupRows2 = await ScalarOn<int>(csCopy, "SELECT COUNT(*) FROM dbo.FoodLogManualBackup");
                Check("27e Script Down: TỪ CHỐI (lỗi 51029) khi còn giá trị manual, 4 cột + dữ liệu còn nguyên; sau khi xuất bảng sao lưu và chủ động xoá giá trị thì Down chạy (cột biến mất), bảng sao lưu giữ lại giá trị",
                    downErr == 51029 && colsAfterRefusedDown == 4 && manualAfterRefusedDown == manualBeforeDown && colsAfterDown2 == 0 && backupRows2 == 2, $"lỗi {downErr}, cột sau khi bị từ chối {colsAfterRefusedDown}, manual nguyên vẹn {manualAfterRefusedDown == manualBeforeDown}, cột sau Down {colsAfterDown2}, dòng sao lưu {backupRows2}");
            }
            finally
            {
                foreach (var n in new[] { dbCopy, dbOld, dbRestored }) await DropDb(n);
                if (File.Exists(bak)) File.Delete(bak);
                foreach (var f in Directory.GetFiles(Path.GetTempPath(), dbRestored + ".*")) { try { File.Delete(f); } catch { /* tệp tạm của LocalDB đã bị giải phóng hay chưa không ảnh hưởng kết quả */ } }
            }
        }
    }

    // JSON thật cho MOS-FE (chỉ in khi NDM_DUMP=1): cùng bộ tuỳ chọn của API (camelCase, enum chuỗi), bọc trong ApiResponse
    if (Environment.GetEnvironmentVariable("NDM_DUMP") == "1")
    {
        var jopts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }; jopts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        void Dump(string name, object o) => Console.WriteLine($"=== JSON {name}\n{JsonSerializer.Serialize(o, jopts)}");
        var u = Guid.NewGuid(); var b = today.AddDays(-5);
        await Seed(u, b, foods: 3, kcalEach: 600, note: false); await Seed(u, b.AddDays(1), mk: 2200, mp: 150, mc: 250, mf: 70, note: false); await Seed(u, b.AddDays(2), mk: 2200, note: false);
        await Seed(u, b.AddDays(3), fasting: true, note: false); await Seed(u, b.AddDays(4), mp: 90, note: false);
        await Seed(u, b.AddDays(5), foods: 3, kcalEach: 600, mk: 0, note: false);
        Dump("GET /api/nutrition-data?limit=90", ApiResponse<NutritionDataResponse>.Ok((await NdmList(u)).Value!));
        var putDay = await Save(u, b.AddDays(-1), 2200); Dump("PUT /api/nutrition-data (calories only: macros null)", ApiResponse<NutritionDataDayModel>.Ok(putDay.Value!));
        var dlx = await Call(uow => new GetDayLogQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetDayLogQuery(b.AddDays(5)), default));
        Dump("GET /api/food-logs?date= (manual 0 + Food Log 1800: fields FE needs)", ApiResponse<object>.Ok(new { logDate = dlx.Value!.LogDate, dlx.Value.IsLogged, dlx.Value.IsFasting, dlx.Value.IsIncomplete, totals = dlx.Value.Totals, effective = dlx.Value.Effective, entriesCount = dlx.Value.Entries.Count }));
        var wkx = await Call(uow => new GetWeekLogQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetWeekLogQuery(b.AddDays(1)), default));
        Dump("GET /api/food-logs/week (one day, new `effective` field)", ApiResponse<object>.Ok(wkx.Value!.Days.Where(x => x.Effective is { HasOverride: true }).Take(1).ToList()));
        var msx = await Call(uow => new GetMacroSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System).Handle(new GetMacroSeriesQuery(b, b.AddDays(5)), default));
        Dump("GET macro series (calories-only day, macro-only day, summary)", ApiResponse<object>.Ok(new { days = msx.Value!.Days.Where(x => x.Date == b.AddDays(2) || x.Date == b.AddDays(4)).ToList(), summary = msx.Value.Summary }));
        var pd = b.AddDays(-3); await AddFood(u, pd, 300); await Save(u, pd, 2000);
        var plx = await Call(uow => new GetPlateNutrientsQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetPlateNutrientsQuery(pd, [new PlateLine(null, null, null, 1, "estimate", 100, 10, 5, 2)]), default));
        Dump("POST /api/food-logs/plate/nutrients (override 2000 + food 300 + plate 100; plate/day trimmed to energy_kcal)", ApiResponse<object>.Ok(new { plx.Value!.Date, plate = plx.Value.Plate.Where(r => r.Code == "energy_kcal"), day = plx.Value.Day.Where(r => r.Code == "energy_kcal"), plx.Value.EffectiveDay }));
        var fd = today.AddDays(-3); await SeedProgram(u, today.AddDays(-12)); await AddFood(u, fd, 405); await Save(u, fd, 2000);
        foreach (var code in new[] { "energy_kcal", "protein_g" })
        {
            var det = (await Call(uow => new GetNutrientDayDetailQueryHandler(uow, new TestUser(u), TimeProvider.System).Handle(new GetNutrientDayDetailQuery(code, fd), default))).Value!;
            Dump($"GET /api/nutrients/{code}/day (calories-only override 2000 + food 405; timing = 24 zeros omitted)", ApiResponse<object>.Ok(new { det.Code, det.Date, det.Amount, det.AmountSource, det.PastWeekAverage, det.PastWeekLoggedDays, contributors = det.Contributors, timingHours = det.Timing.Count, timingTotal = det.Timing.Sum(t => t.Amount) }));
        }
        var dpx = (await Call(uow => new GetDailyProgressQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetDailyProgressQuery(fd, fd), default))).Value!;
        Dump("GET /api/expenditure/daily (calories-only override: macros null)", ApiResponse<object>.Ok(dpx));
        Console.WriteLine("=== JSON error 409 CONCURRENT\n" + (await Map(new DbUpdateConcurrencyException("x"))).Body);
        Console.WriteLine("=== JSON error 400 FASTING_WITH_MANUAL_NUTRITION (handler path: fasting day, then Save with calories)\n" + (await Map(NutritionDataSupport.FastingWithManual())).Body);
        var vb = new ValidationPipelineBehavior<SaveNutritionDataCommand, NutritionDataDayModel>([new SaveNutritionDataCommandValidator()]);
        try { await vb.Handle(new SaveNutritionDataCommand(b, 500, null, null, null, true, null), _ => Task.FromResult<NutritionDataDayModel>(null!), default); }
        catch (Exception e) { Console.WriteLine("=== JSON error 400 FASTING_WITH_MANUAL_NUTRITION (validator path: PUT isFasting:true + calories:500)\n" + (await Map(e)).Body); }
        Console.WriteLine("=== JSON error 409 FASTING_NEEDS_EMPTY_DAY\n" + (await Map(NutritionDataSupport.FastingBlocked())).Body);
        Console.WriteLine("=== JSON error 400 USE_REMOVE_OVERRIDE\n" + (await Map(NutritionDataSupport.UseRemoveOverride())).Body);
        Console.WriteLine("=== JSON error 400 NOTHING_TO_SAVE\n" + (await Map(NutritionDataSupport.NothingToSave())).Body);
    }
}
finally { await Drop(); }

Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAIL");
return failed == 0 ? 0 : 1;

sealed class TestEnv : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "NdmSafetyRepro";
    public string ContentRootPath { get; set; } = ".";
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}

readonly record struct Snap(bool Exists, Guid? LogId, int Live, int Deleted, decimal TotalCal, int Water, int Notes, bool Fasting, bool Incomplete, string? LogNotes, string EntryIds,
    decimal? MCal, decimal? MP, decimal? MC, decimal? MF);

sealed record TestUser(Guid UserId) : IUserContext
{
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}

// Mô hình KHÔNG có 4 cột manual: dựng dữ liệu cũ trên schema trước S29
sealed class LegacyContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<FoodLog>().Ignore(x => x.ManualCaloriesKcal).Ignore(x => x.ManualProteinG).Ignore(x => x.ManualCarbsG).Ignore(x => x.ManualFatG);
    }
}

// Ném lỗi ngay trước câu UPDATE của bảng chỉ định: mô phỏng giao dịch SaveChanges hỏng giữa chừng
sealed class FailOnUpdate(string table) : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    public bool Fired;
    private bool Match(System.Data.Common.DbCommand c) => c.CommandText.Contains("UPDATE") && c.CommandText.Contains($"[{table}]");
    public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> r, CancellationToken ct = default)
    {
        if (Match(c)) { Fired = true; throw new InvalidOperationException("simulated failure before UPDATE " + table); }
        return ValueTask.FromResult(r);
    }
    public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> NonQueryExecutingAsync(System.Data.Common.DbCommand c, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> r, CancellationToken ct = default)
    {
        if (Match(c)) { Fired = true; throw new InvalidOperationException("simulated failure before UPDATE " + table); }
        return ValueTask.FromResult(r);
    }
}
