// E1: Expenditure đứng yên ở Holding dù log nhiều. Mỗi kịch bản = một user mới với program đang chạy (TDEE ban đầu 2381, như ảnh của user), dữ liệu log + cân qua handler THẬT (rebuild dẫn xuất đồng bộ như API),
// rồi đọc đúng thứ FE đọc: GET /expenditure (status, holdingReason), GET /expenditure/insights (holdingReasonCodes, loggedDaysLast7, weighInDaysLast7) và chuỗi series. LocalDB TẠM.
using Application;
using Contract;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_ExpHoldRepro";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";
var audit = new AuditSaveChangesInterceptor(new Microsoft.AspNetCore.Http.HttpContextAccessor(), TimeProvider.System);
var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(audit).Options;
var algoOpt = new AlgorithmOptions { FluxRangeEnabled = true };   // Flux mặc định TẮT ở production; repro bật để kiểm công thức (M5 kiểm mặc định tắt)
var algo = Options.Create(algoOpt);
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var failed = 0;
void Check(string name, bool ok, string detail) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name} — {detail}"); if (!ok) failed++; }

async Task Master(string sql)
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync();
}
Task Drop() => Master($"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");

async Task<T> Req<T>(Func<UnitOfWork, Task<T>> f) { using var c = new ApplicationDbContext(opts); return await f(new UnitOfWork(c)); }
IUserClock Clock(UnitOfWork uow, Guid u) => new UserClock(uow, new TestUser(u), TimeProvider.System);

async Task<Guid> NewUser(int programStartedDaysAgo = 40, int manualTdee = 2381)
{
    var u = Guid.NewGuid();
    using var c = new ApplicationDbContext(opts);
    c.Add(new MacroProgram { Id = Guid.NewGuid(), UserId = u, StartDate = today.AddDays(-programStartedDaysAgo), ExpenditureStartDate = today.AddDays(-programStartedDaysAgo), ManualInitialTdeeKcal = manualTdee, IsActive = true });
    await c.SaveChangesAsync();
    return u;
}
Task LogDay(Guid u, DateOnly d, decimal kcal = 2300) => Req(uow => new QuickAddCommandHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo).Handle(new QuickAddCommand(d, null, null, "food", kcal, 120, 250, 70), default));
Task Weigh(Guid u, DateOnly d, decimal kg) => Req(uow => new UpsertWeightEntryCommandHandler(uow, new TestUser(u), algo, TimeProvider.System).Handle(new UpsertWeightEntryCommand(d, kg, null), default));
Task Incomplete(Guid u, DateOnly d) => Req(uow => new SetDayIncompleteCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new SetDayIncompleteCommand(d, true), default));
async Task LogRange(Guid u, int days, decimal kcal = 2300) { for (var i = days - 1; i >= 0; i--) await LogDay(u, today.AddDays(-i), kcal); }

async Task<(string Status, decimal Tdee, string? Reason, IReadOnlyList<string> Codes, int Logged7, int Weighed7, bool Calibrating, DateOnly? Last)> State(Guid u)
{
    var exp = await Req(uow => new GetExpenditureQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetExpenditureQuery(null, null), default));
    var ins = await Req(uow => new GetExpenditureInsightsQueryHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetExpenditureInsightsQuery(), default));
    return (exp.Current?.Status ?? "(none)", exp.Current?.EstimatedTdeeKcal ?? 0, exp.HoldingReason ?? ins.HoldingReason, ins.HoldingReasonCodes, ins.LoggedDaysLast7, ins.WeighInDaysLast7, ins.IsCalibrating, exp.Current?.SnapshotDate);
}
string Fmt((string Status, decimal Tdee, string? Reason, IReadOnlyList<string> Codes, int Logged7, int Weighed7, bool Calibrating, DateOnly? Last) s)
    => $"status {s.Status}, TDEE {s.Tdee:F0}, logged7 {s.Logged7}, weighIn7 {s.Weighed7}, calibrating {s.Calibrating}, reason [{s.Reason ?? "null"}], codes [{string.Join(",", s.Codes)}], snapshot {s.Last}";

await Drop();
try
{
    using (var c = new ApplicationDbContext(opts))
    {
        await c.Database.MigrateAsync();
        await c.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
    }

    // A) log đủ 7/7, KHÔNG có lần cân nào
    { var u = await NewUser(); await LogRange(u, 7); var s = await State(u);
      Check("A log 7/7 ngày, không cân ⇒ chưa Updating (chuỗi mới chưa từng Updating hiện nhãn Calibrating), lý do NoRecentWeighIn (có câu lý do), TDEE đứng ở 2381", s.Status != "Updating" && s.Tdee == 2381 && s.Codes.Contains("NoRecentWeighIn") && s.Reason is not null, Fmt(s)); }

    // B) log đủ 7/7 + ĐÚNG MỘT lần cân (hôm nay), chưa từng cân trước đó
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today, 80); var s = await State(u);
      Check("B log 7/7 + MỘT lần cân duy nhất ⇒ vẫn Holding (cần ≥ 2 lần cân trong cửa sổ 21 ngày để có độ đổi trend) VÀ API phải nói rõ lý do (code NeedsTwoWeighIns + câu), không để reason = null", s.Status == "Holding" && s.Tdee == 2381 && s.Codes.Contains("NeedsTwoWeighIns") && s.Reason is not null, Fmt(s)); }

    // C) log đủ 7/7 + hai lần cân (6 ngày trước và hôm nay)
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today.AddDays(-6), 80.5m); await Weigh(u, today, 80); var s = await State(u);
      Check("C log 7/7 + 2 lần cân trong 7 ngày ⇒ Updating, TDEE khác 2381, không còn lý do", s.Status == "Updating" && s.Tdee != 2381 && s.Reason is null && s.Codes.Count == 0, Fmt(s)); }

    // D) log đủ 21 ngày + cân cách nhau 20 ngày (một lần trong 7 ngày gần nhất, một lần ở đầu cửa sổ 21 ngày)
    { var u = await NewUser(); await LogRange(u, 21); await Weigh(u, today.AddDays(-20), 81m); await Weigh(u, today, 80m); var s = await State(u);
      Check("D log 21 ngày + cân cách 20 ngày (cân hôm nay + một lần ở đầu cửa sổ) ⇒ Updating", s.Status == "Updating" && s.Tdee != 2381, Fmt(s)); }

    // E) cân chỉ ở quá khứ xa (25 và 30 ngày trước), log đủ 7/7
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today.AddDays(-30), 81m); await Weigh(u, today.AddDays(-25), 80.8m); var s = await State(u);
      Check("E log 7/7 nhưng lần cân gần nhất cách 25 ngày ⇒ chưa Updating, lý do NoRecentWeighIn", s.Status != "Updating" && s.Tdee == 2381 && s.Codes.Contains("NoRecentWeighIn") && s.Reason is not null, Fmt(s)); }

    // J) một lần cân hôm nay + một lần cân cũ NGOÀI cửa sổ 21 ngày (25 ngày trước)
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today.AddDays(-25), 81m); await Weigh(u, today, 80m); var s = await State(u);
      Check("J cân hôm nay + một lần cân 25 ngày trước (ngoài cửa sổ 21 ngày) ⇒ Holding, lý do NeedsTwoWeighIns (không để reason = null)", s.Status == "Holding" && s.Codes.Contains("NeedsTwoWeighIns") && s.Reason is not null, Fmt(s)); }

    // G) mọi ngày bị đánh dấu "chưa đủ" (Incomplete) dù có log, cân đủ
    { var u = await NewUser(); await LogRange(u, 7); for (var i = 0; i < 7; i++) await Incomplete(u, today.AddDays(-i)); await Weigh(u, today.AddDays(-6), 80.5m); await Weigh(u, today, 80); var s = await State(u);
      Check("G 7 ngày có log nhưng đều đánh dấu Incomplete ⇒ chưa Updating, MissingLogs (ngày Incomplete không tính là log hợp lệ), logged7 = 0", s.Status != "Updating" && s.Logged7 == 0 && s.Codes.Contains("MissingLogs"), Fmt(s)); }

    // H) log 4/7 ngày (ngưỡng) + cân hai lần
    { var u = await NewUser(); foreach (var i in new[] { 6, 4, 2, 0 }) await LogDay(u, today.AddDays(-i)); await Weigh(u, today.AddDays(-6), 80.5m); await Weigh(u, today, 80); var s = await State(u);
      Check("H log đúng 4/7 ngày + 2 lần cân ⇒ Updating (ngưỡng 4/7)", s.Status == "Updating", Fmt(s)); }

    // I) program mới bắt đầu 3 ngày trước: giai đoạn Calibrating đóng băng TDEE (FreezeTdeeDuringCalibration mặc định true)
    { var u = await NewUser(programStartedDaysAgo: 3); await LogRange(u, 4); await Weigh(u, today.AddDays(-2), 80.5m); await Weigh(u, today, 80); var s = await State(u);
      Check("I program mới 3 ngày (< CalibratingDays 14): TDEE đóng băng, status Calibrating, IsCalibrating (theo thiết kế, cờ FreezeTdeeDuringCalibration)", s.Status == "Calibrating" && s.Tdee == 2381 && s.Calibrating, Fmt(s)); }

    // K) cân trước ngày bắt đầu tính expenditure của program: bị bỏ qua
    { var u = await NewUser(programStartedDaysAgo: 20); await LogRange(u, 7); await Weigh(u, today.AddDays(-30), 82m); await Weigh(u, today.AddDays(-28), 81.8m); await Weigh(u, today, 80); var s = await State(u);
      Check("K hai lần cân TRƯỚC ngày bắt đầu program bị bỏ qua, chỉ còn một lần cân hôm nay ⇒ Holding, NeedsTwoWeighIns", s.Status == "Holding" && s.Codes.Contains("NeedsTwoWeighIns"), Fmt(s)); }

    // N1) §3 resume do THIẾU CÂN: log 7/7, hai lần cân cũ (10 và 9 ngày trước, TRONG cửa sổ 21 ngày) nhưng không có cân trong 7 ngày ⇒ chưa Updating; MỘT lần cân mới hôm nay ⇒ Updating NGAY và TDEE tính được (ComputeRaw có ≥ 2 điểm trong cửa sổ)
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today.AddDays(-10), 81m); await Weigh(u, today.AddDays(-9), 80.9m); var before = await State(u);
      await Weigh(u, today, 80.2m); var after = await State(u);
      Check("N1 thiếu cân (nutrition đạt, 2 cân cũ trong cửa sổ): chưa có cân trong 7 ngày ⇒ chưa Updating + NoRecentWeighIn; MỘT cân mới ⇒ Updating ngay, TDEE ≠ 2381 (không đòi thêm 3 ngày log)",
          before.Status != "Updating" && before.Codes.Contains("NoRecentWeighIn") && after.Status == "Updating" && after.Tdee != 2381, $"trước: {Fmt(before)} | sau: {Fmt(after)}"); }

    // N2) §3 khoảng hở: cân cũ NGOÀI cửa sổ 21 ngày (40 và 35 ngày trước) + MỘT cân mới ⇒ status machine đủ điều kiện nhưng ComputeRaw chỉ có 1 điểm ⇒ Holding NeedsTwoWeighIns; thêm cân hôm qua ⇒ Updating. Đây là chỗ MacroOS cần ≥ 2 điểm trong 21 ngày (MF 109 chỉ nói "một cân mới", công thức V3 UNKNOWN)
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today.AddDays(-40), 82m); await Weigh(u, today.AddDays(-35), 81.8m); await Weigh(u, today, 80m); var one = await State(u);
      await Weigh(u, today.AddDays(-1), 80.2m); var two = await State(u);
      Check("N2 cân cũ ngoài cửa sổ 21 ngày + MỘT cân mới ⇒ Holding NeedsTwoWeighIns (raw không tính được); cân thêm hôm qua ⇒ Updating",
          one.Status == "Holding" && one.Codes.Contains("NeedsTwoWeighIns") && two.Status == "Updating", $"một cân: {Fmt(one)} | hai cân: {Fmt(two)}"); }

    // L) snapshot hôm nay luôn được dựng lại sau thao tác (không cũ): log + cân ⇒ snapshot mới nhất là hôm nay
    { var u = await NewUser(); await LogRange(u, 7); await Weigh(u, today.AddDays(-6), 80.5m); await Weigh(u, today, 80); var s = await State(u);
      Check("L snapshot mới nhất là HÔM NAY sau khi log + cân (rebuild dẫn xuất không bị bỏ sót)", s.Last == today, $"snapshot {s.Last}, hôm nay {today}"); }

    // ===== E3: Flux Range qua handler THẬT (series + bucket Week, GET /expenditure, insights): một điểm tính, bao chứa kcal, null khi chưa đủ dữ liệu =====
    {
        var u = await NewUser(programStartedDaysAgo: 40);
        for (var i = 39; i >= 0; i--) await LogDay(u, today.AddDays(-i), 2300 + (i % 4) * 40);
        for (var i = 39; i >= 0; i -= 3) await Weigh(u, today.AddDays(-i), 82m - 0.05m * (39 - i) + (i % 2 == 0 ? 0.2m : -0.2m));
        var series = await Req(uow => new GetExpenditureSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetExpenditureSeriesQuery(null, null, true, MetricGranularity.Week), default));
        var exp = await Req(uow => new GetExpenditureQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetExpenditureQuery(null, null), default));
        var ins = await Req(uow => new GetExpenditureInsightsQueryHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new GetExpenditureInsightsQuery(), default));
        var banded = series.Days.Where(d => d.FluxLowKcal is not null).ToList();
        var last = series.Days[^1];
        Check("M1 series: có dải ở các ngày Updating, mọi ngày có dải thoả FluxLow ≤ ExpenditureKcal ≤ FluxHigh, không dải giả (ngày Calibrating null), FluxModel = MACROOS_FLUX_V1",
            banded.Count > 0 && banded.All(d => d.FluxLowKcal <= d.ExpenditureKcal && d.ExpenditureKcal <= d.FluxHighKcal) && series.Days.Where(d => d.IsCalibrating && d.Status == "Calibrating").All(d => d.FluxLowKcal is null) && series.FluxModel == "MACROOS_FLUX_V1",
            $"{banded.Count}/{series.Days.Count} ngày có dải; hôm nay {last.ExpenditureKcal} [{last.FluxLowKcal}..{last.FluxHighKcal}] {last.Status}; model {series.FluxModel}");
        Check("M2 bucket Week: Low ≤ Average ≤ High khi có dải, bucket không có dải ⇒ null",
            series.Buckets.Count > 0 && series.Buckets.All(b => (b.FluxLowKcal is null) == (b.FluxHighKcal is null) && (b.FluxLowKcal is null || (b.FluxLowKcal <= b.Average && b.Average <= b.FluxHighKcal))) && series.Buckets.Any(b => b.FluxLowKcal is not null),
            string.Join(" | ", series.Buckets.TakeLast(3).Select(b => $"{b.Start:MM-dd} {b.FluxLowKcal}/{b.Average}/{b.FluxHighKcal}")));
        var expByDate = exp.Points.ToDictionary(p => p.SnapshotDate);
        Check("M3 MỘT điểm tính: GET /expenditure (Points/Current), insights và series cho CÙNG dải ở cùng ngày; Points thoả Low ≤ TDEE ≤ High",
            exp.Points.Where(p => p.FluxLowKcal is not null).All(p => p.FluxLowKcal <= p.EstimatedTdeeKcal && p.EstimatedTdeeKcal <= p.FluxHighKcal)
            && series.Days.Where(d => expByDate.ContainsKey(d.Date)).All(d => expByDate[d.Date].FluxLowKcal == d.FluxLowKcal && expByDate[d.Date].FluxHighKcal == d.FluxHighKcal)
            && ins.FluxLowKcal == last.FluxLowKcal && ins.FluxHighKcal == last.FluxHighKcal && exp.Current?.FluxLowKcal == last.FluxLowKcal && exp.FluxModel == "MACROOS_FLUX_V1",
            $"hôm nay series [{last.FluxLowKcal}..{last.FluxHighKcal}] / expenditure [{exp.Current?.FluxLowKcal}..{exp.Current?.FluxHighKcal}] / insights [{ins.FluxLowKcal}..{ins.FluxHighKcal}]");

        // Dữ liệu chưa đủ (một lần cân, chưa từng Updating) ⇒ KHÔNG có dải
        var u2 = await NewUser(); await LogRange(u2, 7); await Weigh(u2, today, 80);
        var s2 = await Req(uow => new GetExpenditureSeriesQueryHandler(uow, new TestUser(u2), TimeProvider.System, algo).Handle(new GetExpenditureSeriesQuery(null, null, true, MetricGranularity.Week), default));
        var i2 = await Req(uow => new GetExpenditureInsightsQueryHandler(uow, new TestUser(u2), TimeProvider.System, algo).Handle(new GetExpenditureInsightsQuery(), default));
        Check("M4 chưa từng Updating (một lần cân): series/bucket/insights KHÔNG có dải (null, FluxModel null)",
            s2.Days.Count > 0 && s2.Days.All(d => d.FluxLowKcal is null && d.FluxHighKcal is null) && s2.Buckets.All(b => b.FluxLowKcal is null) && s2.FluxModel is null && i2.FluxLowKcal is null && i2.FluxModel is null,
            $"{s2.Days.Count} ngày, model {s2.FluxModel ?? "null"}");

        // Mặc định production (FluxRangeEnabled = false, quyết định user 2026-10-26): CÙNG dữ liệu như M1 nhưng API trả Flux null ở mọi nơi
        var off = Microsoft.Extensions.Options.Options.Create(new AlgorithmOptions());
        var sOff = await Req(uow => new GetExpenditureSeriesQueryHandler(uow, new TestUser(u), TimeProvider.System, off).Handle(new GetExpenditureSeriesQuery(null, null, true, MetricGranularity.Week), default));
        var eOff = await Req(uow => new GetExpenditureQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, off).Handle(new GetExpenditureQuery(null, null), default));
        var iOff = await Req(uow => new GetExpenditureInsightsQueryHandler(uow, new TestUser(u), TimeProvider.System, off).Handle(new GetExpenditureInsightsQuery(), default));
        Check("M5 Flux mặc định TẮT: series/bucket/GET expenditure/insights đều null (dải + FluxModel), TDEE/status vẫn như khi bật",
            sOff.Days.All(d => d.FluxLowKcal is null && d.FluxHighKcal is null) && sOff.Buckets.All(b => b.FluxLowKcal is null && b.FluxHighKcal is null) && sOff.FluxModel is null
            && eOff.FluxModel is null && eOff.Points.All(p => p.FluxLowKcal is null && p.FluxHighKcal is null) && iOff.FluxLowKcal is null && iOff.FluxHighKcal is null && iOff.FluxModel is null
            && sOff.Days.Select(d => (d.Date, d.ExpenditureKcal, d.Status)).SequenceEqual(series.Days.Select(d => (d.Date, d.ExpenditureKcal, d.Status))),
            $"{sOff.Days.Count} ngày, model {sOff.FluxModel ?? "null"}");
    }

    // ===== §7 (user 2026-10-26): log TƯƠNG LAI vẫn được phép; KHÔNG vào Expenditure hiện tại, KHÔNG là ngày nutrition hoàn tất trước khi ngày đến, KHÔNG ảnh hưởng check-in, ngày theo MÚI GIỜ user. Đồng hồ cố định, DB cô lập =====
    {
        var mt = new MutableTime();
        IUserClock ClockAt(UnitOfWork uow, Guid u) => new UserClock(uow, new TestUser(u), mt);
        async Task<Guid> UserTz(string tz, DateOnly start)
        {
            var u = Guid.NewGuid();
            using var c = new ApplicationDbContext(opts);
            c.Add(new UserProfile { Id = Guid.NewGuid(), UserId = u, FirstName = "T", LastName = "T", TimeZone = tz });
            var prog = new MacroProgram { Id = Guid.NewGuid(), UserId = u, StartDate = start, ExpenditureStartDate = start, ManualInitialTdeeKcal = 2381, IsActive = true };
            prog.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = start, CaloriesTarget = 2400, ProteinTarget = 150, CarbsTarget = 250, FatTarget = 70 });
            c.Add(prog);
            await c.SaveChangesAsync();
            return u;
        }
        Task LogAt(Guid u, DateOnly d, decimal kcal) => Req(uow => new QuickAddCommandHandler(ClockAt(uow, u), uow, new TestUser(u), mt, algo).Handle(new QuickAddCommand(d, null, null, "food", kcal, 120, 250, 70), default));
        Task WeighAt(Guid u, DateOnly d, decimal kg) => Req(uow => new UpsertWeightEntryCommandHandler(uow, new TestUser(u), algo, mt).Handle(new UpsertWeightEntryCommand(d, kg, null), default));
        async Task<(string Status, decimal Tdee, int Logged7, int Weighed7, DateOnly? Last, int Snaps, int DailySnaps, DateOnly? MaxSnap, int SeriesDays, DateOnly? SeriesLast, int PreviewDays, int PreviewAvg)> Snap(Guid u)
        {
            var exp = await Req(uow => new GetExpenditureQueryHandler(ClockAt(uow, u), uow, new TestUser(u), mt, algo).Handle(new GetExpenditureQuery(null, null), default));
            var ins = await Req(uow => new GetExpenditureInsightsQueryHandler(uow, new TestUser(u), mt, algo).Handle(new GetExpenditureInsightsQuery(), default));
            var ser = await Req(uow => new GetExpenditureSeriesQueryHandler(uow, new TestUser(u), mt, algo).Handle(new GetExpenditureSeriesQuery(null, null, true, MetricGranularity.Week), default));
            var pv = await Req(uow => new GetCheckInPreviewQueryHandler(ClockAt(uow, u), uow, new TestUser(u), mt, algo).Handle(new GetCheckInPreviewQuery(), default));
            using var c = new ApplicationDbContext(opts);
            var snaps = await c.Set<ExpenditureSnapshot>().Where(s => s.UserId == u).Select(s => s.SnapshotDate).ToListAsync();
            var daily = await c.Set<DailyProgressSnapshot>().CountAsync(s => s.UserId == u);
            return (exp.Current?.Status ?? "(none)", exp.Current?.EstimatedTdeeKcal ?? 0, ins.LoggedDaysLast7, ins.WeighInDaysLast7, exp.Current?.SnapshotDate, snaps.Count, daily, snaps.Count == 0 ? null : snaps.Max(),
                ser.Days.Count, ser.Days.Count == 0 ? null : ser.Days[^1].Date, pv.Week.DaysLogged, (int)pv.Week.AvgCaloriesIn);
        }

        // O1/O2/O3: UTC; "hôm nay" = 2026-10-26 (đồng hồ 12:00Z). Ngày cũ có dữ liệu đủ ⇒ Updating. Rồi log NGÀY TƯƠNG LAI (QuickAdd 27/10, Copy sang 28/10 và 30/10)
        mt.Now = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var d0 = new DateOnly(2026, 10, 26);
        var f1 = await UserTz("UTC", d0.AddDays(-20));
        for (var i = 7; i >= 0; i--) await LogAt(f1, d0.AddDays(-i), 2300);
        await WeighAt(f1, d0.AddDays(-6), 80.5m); await WeighAt(f1, d0, 80m);
        var b = await Snap(f1);
        await LogAt(f1, d0.AddDays(1), 6000);
        var cp1 = await Req(uow => new CopyFoodLogCommandHandler(uow, new TestUser(f1), mt, algo).Handle(new CopyFoodLogCommand(d0, d0.AddDays(2), null, null), default));
        var cp2 = await Req(uow => new CopyFoodLogCommandHandler(uow, new TestUser(f1), mt, algo).Handle(new CopyFoodLogCommand(d0, d0.AddDays(4), null, null), default));
        var a = await Snap(f1);
        using (var c = new ApplicationDbContext(opts))
        {
            var futureEntries = await c.Set<FoodLogEntry>().CountAsync(e => e.FoodLog.UserId == f1 && e.FoodLog.LogDate > d0);
            Check("O1 log tương lai (QuickAdd 27/10 + Copy sang 28/10, 30/10) vẫn được lưu nhưng KHÔNG vào Expenditure: status/TDEE/logged7/weighIn7 và số snapshot/daily-progress không đổi, không snapshot sau hôm nay, series kết thúc hôm nay",
                b.Status == "Updating" && a.Status == b.Status && a.Tdee == b.Tdee && a.Logged7 == b.Logged7 && a.Weighed7 == b.Weighed7 && a.Snaps == b.Snaps && a.DailySnaps == b.DailySnaps
                && a.MaxSnap == d0 && a.SeriesLast == d0 && b.SeriesLast == d0 && cp1 > 0 && cp2 > 0 && futureEntries >= 3, $"trước {b.Status}/{b.Tdee:F0}/logged7 {b.Logged7}/snap {b.Snaps}/max {b.MaxSnap}; sau {a.Status}/{a.Tdee:F0}/logged7 {a.Logged7}/snap {a.Snaps}/max {a.MaxSnap}; entry tương lai {futureEntries}");
        }
        Check("O2 check-in không đổi vì log tương lai: số ngày log và kcal trung bình của 7 ngày xem lại không đổi (preview trước = sau)", a.PreviewDays == b.PreviewDays && a.PreviewAvg == b.PreviewAvg, $"DaysLogged trước {b.PreviewDays} sau {a.PreviewDays}; avg {b.PreviewAvg} → {a.PreviewAvg}");

        // O3: ngày tương lai chưa đến KHÔNG là ngày nutrition hoàn tất: user B chỉ có log tương lai (không log nào ≤ hôm nay) ⇒ logged7 = 0 và chưa Updating dù ngày đó có 2300 kcal
        var f2 = await UserTz("UTC", d0.AddDays(-20));
        await WeighAt(f2, d0.AddDays(-6), 80.5m); await WeighAt(f2, d0, 80m);
        for (var i = 1; i <= 5; i++) await LogAt(f2, d0.AddDays(i), 2300);
        var fb = await Snap(f2);
        Check("O3 chỉ có log tương lai (5 ngày sau hôm nay, 2300 kcal) + 2 lần cân ⇒ logged7 = 0 và KHÔNG Updating (ngày chưa đến không là ngày hoàn tất)", fb.Logged7 == 0 && fb.Status != "Updating" && fb.Tdee == 2381, $"{fb.Status}/TDEE {fb.Tdee:F0}/logged7 {fb.Logged7}/max snap {fb.MaxSnap}");

        // O4a: múi giờ VN (UTC+7): 18:30Z ngày 26/10 = 01:30 ngày 27/10 LOCAL ⇒ "hôm nay" của user là 27/10; log 27/10 LÀ hôm nay (snapshot mới nhất = 27/10), log 28/10 là tương lai
        mt.Now = new DateTimeOffset(2026, 10, 26, 18, 30, 0, TimeSpan.Zero);
        var vnToday = new DateOnly(2026, 10, 27);
        var vn = await UserTz("Asia/Ho_Chi_Minh", vnToday.AddDays(-20));
        for (var i = 6; i >= 1; i--) await LogAt(vn, vnToday.AddDays(-i), 2300);
        await WeighAt(vn, vnToday.AddDays(-6), 80.5m); await WeighAt(vn, vnToday, 80m);
        await LogAt(vn, vnToday, 2300);
        var v1 = await Snap(vn);
        await LogAt(vn, vnToday.AddDays(1), 5000);
        var v2 = await Snap(vn);
        Check("O4a user UTC+7 lúc 18:30Z (= 01:30 ngày 27/10 local): hôm nay local = 27/10 ⇒ snapshot mới nhất 27/10 (không lệch về 26/10 theo UTC); log 28/10 (tương lai local) không đổi gì",
            v1.Last == vnToday && v1.MaxSnap == vnToday && v1.Status == "Updating" && v2.Last == vnToday && v2.MaxSnap == vnToday && v2.Tdee == v1.Tdee && v2.Logged7 == v1.Logged7 && v2.Snaps == v1.Snaps, $"trước last {v1.Last:MM-dd} {v1.Status}; sau last {v2.Last:MM-dd}, TDEE {v1.Tdee:F0}→{v2.Tdee:F0}");

        // O4b: múi giờ LA (UTC−7): 03:00Z ngày 27/10 = 20:00 ngày 26/10 local ⇒ ngày 27/10 đã là hôm nay theo UTC nhưng vẫn là NGÀY MAI của user
        mt.Now = new DateTimeOffset(2026, 10, 27, 3, 0, 0, TimeSpan.Zero);
        var laToday = new DateOnly(2026, 10, 26);
        var la = await UserTz("America/Los_Angeles", laToday.AddDays(-20));
        for (var i = 6; i >= 0; i--) await LogAt(la, laToday.AddDays(-i), 2300);
        await WeighAt(la, laToday.AddDays(-6), 80.5m); await WeighAt(la, laToday, 80m);
        var l1 = await Snap(la);
        await LogAt(la, laToday.AddDays(1), 5000);   // 27/10: đã là hôm nay theo UTC (03:00Z) nhưng vẫn là NGÀY MAI của user LA
        var l2 = await Snap(la);
        Check("O4b user LA lúc 03:00Z ngày 27/10 (= 20:00 ngày 26/10 local): hôm nay local = 26/10 ⇒ log 27/10 là tương lai, không đổi Expenditure (dù theo UTC đã là hôm nay)",
            l1.Last == laToday && l2.Last == laToday && l2.MaxSnap == laToday && l2.Tdee == l1.Tdee && l2.Logged7 == l1.Logged7 && l2.Snaps == l1.Snaps && l1.Status == "Updating", $"trước last {l1.Last:MM-dd} {l1.Status}/{l1.Tdee:F0}; sau last {l2.Last:MM-dd} {l2.Status}/{l2.Tdee:F0}, logged7 {l1.Logged7}→{l2.Logged7}");
    }
}
finally { await Drop(); }

Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAIL");
return failed == 0 ? 0 : 1;

sealed record TestUser(Guid UserId) : IUserContext
{
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}

sealed class MutableTime : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}
