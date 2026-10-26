// E1 what-if (READ-ONLY với production: chỉ đọc file đã trích bằng SELECT được user duyệt). Dựng lại dữ liệu vào LocalDB TẠM, chạy handler THẬT hai lần:
//   FROZEN  = FreezeTdeeDuringCalibration true  (hành vi production hiện tại)
//   LEARN   = FreezeTdeeDuringCalibration false (học từ ngày 1, #3 chờ duyệt)
// cộng các biến thể độ nhạy với ngày log rất ít kcal. Tham số: <thư mục chứa qA.txt (cân), qC.txt (log theo ngày)>. Ngày được DỊCH để ngày dữ liệu cuối = hôm nay của harness (thuật toán bất biến theo dịch ngày).
using Application;
using Contract;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_ExpWhatIf";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";
var audit = new AuditSaveChangesInterceptor(new Microsoft.AspNetCore.Http.HttpContextAccessor(), TimeProvider.System);
var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(audit).Options;
var dir = args.Length > 0 ? args[0] : throw new ArgumentException("cần thư mục chứa qA.txt, qC.txt");

async Task Master(string sql)
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync();
}
Task Drop() => Master($"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");
async Task<T> Req<T>(Func<UnitOfWork, Task<T>> f) { using var c = new ApplicationDbContext(opts); return await f(new UnitOfWork(c)); }

static List<string[]> Rows(string path) => File.ReadAllLines(path).Skip(2).Where(l => l.Contains('|')).Select(l => l.Split('|')).ToList();
var inv = System.Globalization.CultureInfo.InvariantCulture;
var weights = Rows(Path.Combine(dir, "qA.txt")).Where(r => r[3].Trim() == "0").Select(r => (Date: DateOnly.Parse(r[0].Trim()), Kg: decimal.Parse(r[1].Trim(), inv))).Where(w => w.Date >= new DateOnly(2026, 10, 17)).OrderBy(w => w.Date).ToList();
var logs = Rows(Path.Combine(dir, "qC.txt")).Select(r => (Date: DateOnly.Parse(r[0].Trim()), Fasting: r[1].Trim() == "1", Kcal: decimal.Parse(r[5].Trim(), inv), Live: int.Parse(r[4].Trim()))).OrderBy(l => l.Date).ToList();
var programStart = new DateOnly(2026, 10, 17);
var lastData = new DateOnly(2026, 10, 26);
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var shift = today.DayNumber - lastData.DayNumber;
DateOnly S(DateOnly d) => d.AddDays(shift);
DateOnly Unshift(DateOnly d) => d.AddDays(-shift);

IUserClock Clock(UnitOfWork uow, Guid u) => new UserClock(uow, new TestUser(u), TimeProvider.System);

async Task<Guid> Build(IOptions<AlgorithmOptions> algo, Func<DateOnly, decimal, decimal?> kcalFor, Func<DateOnly, bool>? incomplete = null)
{
    var u = Guid.NewGuid();
    using (var c = new ApplicationDbContext(opts))
    {
        c.Add(new MacroProgram { Id = Guid.NewGuid(), UserId = u, StartDate = S(programStart), ExpenditureStartDate = S(programStart), ManualInitialTdeeKcal = 2381, IsActive = true });
        await c.SaveChangesAsync();
    }
    foreach (var d in weights.Select(w => w.Date).Concat(logs.Select(l => l.Date)).Distinct().Where(d => S(d) <= today).OrderBy(d => d))
    {
        if (weights.FirstOrDefault(w => w.Date == d) is { Kg: > 0 } w)
            await Req(uow => new UpsertWeightEntryCommandHandler(uow, new TestUser(u), algo, TimeProvider.System).Handle(new UpsertWeightEntryCommand(S(d), w.Kg, null), default));
        if (logs.FirstOrDefault(l => l.Date == d) is { Live: > 0 } l && kcalFor(d, l.Kcal) is { } k && k > 0)
            await Req(uow => new QuickAddCommandHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo).Handle(new QuickAddCommand(S(d), null, null, "food", k, Math.Round(k * 0.2m / 4m, 1), Math.Round(k * 0.5m / 4m, 1), Math.Round(k * 0.3m / 9m, 1)), default));
        // Ngày user XÁC NHẬN chưa đủ (endpoint PUT /nutrition-data/{date}/incomplete); KHÔNG bao giờ tự đánh dấu theo ngưỡng kcal
        if (incomplete?.Invoke(d) == true && logs.FirstOrDefault(l => l.Date == d) is { Live: > 0 })
            await Req(uow => new SetDayIncompleteCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new SetDayIncompleteCommand(S(d), true), default));
    }
    return u;
}

async Task Report(string title, AlgorithmOptions opt, Func<DateOnly, decimal, decimal?> kcalFor, Func<DateOnly, bool>? incomplete = null)
{
    var algo = Options.Create(opt);
    var u = await Build(algo, kcalFor, incomplete);
    Console.WriteLine($"\n### {title}");
    Console.WriteLine("date | tdee | status | reason");
    using var c = new ApplicationDbContext(opts);
    var snaps = await c.Set<ExpenditureSnapshot>().Where(s => s.UserId == u).OrderBy(s => s.SnapshotDate).ToListAsync();
    foreach (var s in snaps)
    {
        var (days, window) = await Req(uow => ExpenditureDiagnostics.LoadAsync(uow, u, s.SnapshotDate, S(programStart), opt, default));
        var status = s.Status.ToString();
        int? left = opt.FreezeTdeeDuringCalibration && status == "Calibrating" ? Math.Max(0, opt.CalibratingDays - (s.SnapshotDate.DayNumber - S(programStart).DayNumber)) : null;
        var reason = status == "Updating" ? "(updating)" : ExpenditureDiagnostics.Diagnose(days, window, s.SnapshotDate, opt.ExpenditureWindowDays, left).Reason ?? "-";
        Console.WriteLine($"{Unshift(s.SnapshotDate):MM-dd} | {s.EstimatedTdeeKcal:F0} | {status} | {reason}");
    }
    var tr = await c.Set<WeightEntry>().Where(w => w.UserId == u).OrderBy(w => w.LogDate).Select(w => new { w.LogDate, w.TrendWeightKg }).ToListAsync();
    Console.WriteLine("trend(harness): " + string.Join(", ", tr.Select(t => $"{Unshift(t.LogDate):MM-dd}={t.TrendWeightKg:F2}")));
}

await Drop();
try
{
    using (var c = new ApplicationDbContext(opts))
    {
        await c.Database.MigrateAsync();
        await c.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
    }
    Console.WriteLine($"weights {weights.Count}, log days with live entries {logs.Count(l => l.Live > 0)}, shift {shift} days");
    static bool Tiny(DateOnly d) => d >= new DateOnly(2026, 10, 20) && d <= new DateOnly(2026, 10, 22);
    var frozen = new AlgorithmOptions();
    var learn = new AlgorithmOptions { FreezeTdeeDuringCalibration = false };
    await Report("A) FROZEN (production now: freeze 14 days)", frozen, (_, k) => k);
    await Report("B) LEARN FROM DAY 1, data as is", learn, (_, k) => k);
    await Report("C) LEARN FROM DAY 1, tiny days 10/20-10/22 ignored (not logged)", learn, (d, k) => Tiny(d) ? null : k);
    await Report("D) LEARN FROM DAY 1, tiny days 10/20-10/22 each = 2300 kcal (if fully logged)", learn, (d, k) => Tiny(d) ? 2300 : k);
    await Report("E) LEARN FROM DAY 1, tiny days x10 (233->2334, 64.5->645, 232.5->2325)", learn, (d, k) => Tiny(d) ? k * 10 : k);
    // §2 (user 2026-10-26): input đã xử lý chất lượng = user XÁC NHẬN 3 ngày 10/20-10/22 chưa đủ (IsIncomplete); Day 1 = học từ ngày 1, Day 7 = đóng băng 7 ngày (CalibratingDays=7)
    var day7 = new AlgorithmOptions { CalibratingDays = 7 };
    await Report("F) LEARN FROM DAY 1, tiny days CONFIRMED incomplete by the user", learn, (_, k) => k, Tiny);
    await Report("G) DAY 7 (freeze 7 days), data as is (tiny days counted)", day7, (_, k) => k);
    await Report("H) DAY 7 (freeze 7 days), tiny days CONFIRMED incomplete", day7, (_, k) => k, Tiny);
}
finally { await Drop(); }

sealed record TestUser(Guid UserId) : IUserContext
{
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}
