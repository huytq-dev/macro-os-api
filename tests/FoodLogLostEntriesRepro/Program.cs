// P0 "4 món biến mất": log N món vào CÙNG một ngày bằng mọi đường FE dùng (POST entries tuần tự/song song, quick-add, batch, bulk copy). Handler THẬT, mỗi request một DbContext + UnitOfWork như prod, LocalDB TẠM.
// Mỗi kịch bản kiểm: số request OK, số entry sống của ngày (đọc bằng GetDayLog như FE), số dòng FoodLogs của (user, ngày) và tổng ngày khớp entry.
using Application;
using Contract;
using Domain;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_FoodLogRepro";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";   // MARS như prod
var audit = new AuditSaveChangesInterceptor(new Microsoft.AspNetCore.Http.HttpContextAccessor(), TimeProvider.System);
var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).AddInterceptors(audit).Options;
var algo = Options.Create(new AlgorithmOptions());
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var failed = 0;
void Check(string name, bool ok, string detail) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name} — {detail}"); if (!ok) failed++; }

async Task Drop()
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand();
    cmd.CommandText = $"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END";
    await cmd.ExecuteNonQueryAsync();
}

// Một request = một DbContext mới; trả (OK?, mô tả lỗi ngắn)
async Task<(bool Ok, string Error, T? Value)> Req<T>(Func<UnitOfWork, Task<T>> f)
{
    using var c = new ApplicationDbContext(opts);
    try { return (true, "", await f(new UnitOfWork(c))); }
    catch (Exception e) { var x = e.InnerException ?? e; return (false, $"{e.GetType().Name}: {x.Message[..Math.Min(90, x.Message.Length)]}", default); }
}
IUserClock Clock(UnitOfWork uow, Guid u) => new UserClock(uow, new TestUser(u), TimeProvider.System);

var foods = new List<Guid>();
async Task Seed()
{
    using var c = new ApplicationDbContext(opts);
    await c.Database.MigrateAsync();
    await c.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");   // user thử không cần dòng Users thật; UNIQUE (UserId, LogDate) vẫn nguyên
    await NutrientData.SeedAsync(c);
    for (var i = 0; i < 12; i++)
    {
        var f = new FoodItem { Id = Guid.NewGuid(), Name = "Repro food " + i, Category = default, Source = FoodSource.System, CaloriesPer100g = 100 + i, ProteinPer100g = 10, CarbsPer100g = 20, FatPer100g = 5 };
        c.Add(f); foods.Add(f.Id);
    }
    await c.SaveChangesAsync();
}

// Chạy handler QUA pipeline behavior ghi Food Log như API (FoodLogWriteLockBehavior); NOLOCK=1 bỏ behavior để chứng minh lỗi trước khi sửa (fail-before)
var useLock = Environment.GetEnvironmentVariable("NOLOCK") != "1";
Task<(bool Ok, string Error, TRes? Value)> Piped<TReq, TRes>(Guid u, TReq req, Func<UnitOfWork, IRequestHandler<TReq, TRes>> make) where TReq : IRequest<TRes>
    => Req(async uow =>
    {
        var handler = make(uow);
        if (!useLock) return await handler.Handle(req, default);
        return await new FoodLogWriteLockBehavior<TReq, TRes>(uow, new TestUser(u)).Handle(req, ct => handler.Handle(req, ct), default);
    });

Task<(bool Ok, string Error, Guid Value)> AddEntry(Guid u, DateOnly d, Guid food, decimal grams = 100)
    => Piped(u, new AddFoodLogEntryCommand(d, MealType.Lunch, new TimeOnly(12, 0), food, null, grams), uow => new AddFoodLogEntryCommandHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo));
Task<(bool Ok, string Error, Guid Value)> Quick(Guid u, DateOnly d, string name, decimal kcal)
    => Piped(u, new QuickAddCommand(d, null, null, name, kcal, 10, 20, 5), uow => new QuickAddCommandHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo));
Task<(bool Ok, string Error, BatchEntriesResponse? Value)> Batch(Guid u, DateOnly d, string key, params Guid[] items)
    => Piped(u, new AddEntriesBatchCommand(key, d, MealType.Lunch, new TimeOnly(12, 0), "repro", items.Select(f => new BatchEntryItem(f, null, null, 100)).ToList()), uow => new AddEntriesBatchCommandHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System, algo));
Task<(bool Ok, string Error, MediatR.Unit Value)> Delete(Guid u, Guid entryId)
    => Piped(u, new DeleteFoodLogEntryCommand(entryId), uow => new DeleteFoodLogEntryCommandHandler(uow, new TestUser(u), TimeProvider.System, algo));
Task<(bool Ok, string Error, MediatR.Unit Value)> EditGrams(Guid u, Guid entryId, decimal grams)
    => Piped(u, new UpdateFoodLogEntryCommand(entryId, null, null, null, grams), uow => new UpdateFoodLogEntryCommandHandler(uow, new TestUser(u), TimeProvider.System, algo));

// Trạng thái ngày như FE thấy: entry qua GetDayLog + số dòng FoodLogs + entry sống của user ở mọi ngày
async Task<(int FeSees, int LogRows, int LiveAllDays, decimal DayKcal, decimal EntriesKcal)> State(Guid u, DateOnly d)
{
    var day = (await Req(uow => new GetDayLogQueryHandler(Clock(uow, u), uow, new TestUser(u), TimeProvider.System).Handle(new GetDayLogQuery(d), default))).Value!;
    using var c = new ApplicationDbContext(opts);
    var rows = await c.Set<FoodLog>().IgnoreQueryFilters().CountAsync(l => l.UserId == u && l.LogDate == d);
    var live = await c.Set<FoodLogEntry>().CountAsync(e => e.FoodLog.UserId == u);
    return (day.Entries.Count, rows, live, day.Totals.Calories, day.Entries.Sum(e => e.Calories));
}
string Fmt((int FeSees, int LogRows, int LiveAllDays, decimal DayKcal, decimal EntriesKcal) s, params (bool Ok, string Error, object? Value)[] r)
    => $"requests ok {r.Count(x => x.Ok)}/{r.Length}{(r.Any(x => !x.Ok) ? " [" + string.Join(" | ", r.Where(x => !x.Ok).Select(x => x.Error).Distinct()) + "]" : "")}; FE sees {s.FeSees}, FoodLog rows {s.LogRows}, entries all days {s.LiveAllDays}, day kcal {s.DayKcal} vs entries {s.EntriesKcal}";

await Drop();
try
{
    await Seed();
    var d = today.AddDays(-1);

    // 1) tuần tự (đường FE logPlate hiện tại): 4 POST entries lần lượt
    { var u = Guid.NewGuid(); var rs = new List<(bool, string, object?)>(); for (var i = 0; i < 4; i++) { var r = await AddEntry(u, d, foods[i]); rs.Add((r.Ok, r.Error, r.Value)); } var s = await State(u, d);
      Check("1 Tuần tự 4 POST entries cùng ngày mới ⇒ đủ 4 entry", s.FeSees == 4 && s.LogRows == 1 && rs.All(x => x.Item1) && s.DayKcal == s.EntriesKcal, Fmt(s, rs.Select(x => (x.Item1, x.Item2, x.Item3)).ToArray())); }

    // 2) song song 4 POST entries, NGÀY MỚI (chưa có dòng FoodLog): chạy 5 vòng để bắt đua
    { var bad = new List<string>(); var worst = 4;
      for (var round = 0; round < 5; round++)
      {
          var u = Guid.NewGuid();
          var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(i => AddEntry(u, d, foods[i])));
          var s = await State(u, d); worst = Math.Min(worst, s.FeSees);
          if (s.FeSees != 4 || rs.Any(x => !x.Ok) || s.DayKcal != s.EntriesKcal) bad.Add($"vòng {round}: " + Fmt(s, rs.Select(x => (x.Ok, x.Error, (object?)x.Value)).ToArray()));
      }
      Check("2 SONG SONG 4 POST entries cùng ngày mới (5 vòng) ⇒ cả 4 request OK và FE thấy đủ 4 entry, tổng ngày khớp", bad.Count == 0, bad.Count == 0 ? "5/5 vòng đủ 4" : $"{bad.Count}/5 vòng sai, ít nhất FE thấy {worst}; " + bad[0]); }

    // 3) song song 4 POST entries, NGÀY ĐÃ CÓ dòng FoodLog + 1 entry
    { var bad = new List<string>();
      for (var round = 0; round < 5; round++)
      {
          var u = Guid.NewGuid(); await AddEntry(u, d, foods[8]);
          var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(i => AddEntry(u, d, foods[i])));
          var s = await State(u, d);
          if (s.FeSees != 5 || rs.Any(x => !x.Ok) || s.DayKcal != s.EntriesKcal) bad.Add($"vòng {round}: " + Fmt(s, rs.Select(x => (x.Ok, x.Error, (object?)x.Value)).ToArray()));
      }
      Check("3 SONG SONG 4 POST entries vào ngày ĐÃ có dòng FoodLog (5 vòng) ⇒ 5 entry, mọi request OK, tổng ngày khớp entry", bad.Count == 0, bad.Count == 0 ? "5/5 vòng đủ 5" : $"{bad.Count}/5 vòng sai; " + bad[0]); }

    // 4) song song 4 quick-add ngày mới
    { var bad = new List<string>();
      for (var round = 0; round < 5; round++)
      {
          var u = Guid.NewGuid();
          var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(i => Quick(u, d, "quick " + i, 100 + i)));
          var s = await State(u, d);
          if (s.FeSees != 4 || rs.Any(x => !x.Ok) || s.DayKcal != s.EntriesKcal) bad.Add($"vòng {round}: " + Fmt(s, rs.Select(x => (x.Ok, x.Error, (object?)x.Value)).ToArray()));
      }
      Check("4 SONG SONG 4 quick-add cùng ngày mới (5 vòng) ⇒ đủ 4 entry, mọi request OK", bad.Count == 0, bad.Count == 0 ? "5/5 vòng đủ 4" : $"{bad.Count}/5 vòng sai; " + bad[0]); }

    // 5) một batch 4 món (một giao dịch) + replay cùng IdempotencyKey không nhân đôi, không mất
    { var u = Guid.NewGuid(); var b1 = await Batch(u, d, "key-1", foods[0], foods[1], foods[2], foods[3]); var b2 = await Batch(u, d, "key-1", foods[0], foods[1], foods[2], foods[3]); var s = await State(u, d);
      Check("5 Batch 4 món + replay cùng key ⇒ đúng 4 entry (không nhân đôi, không mất), replay trả cùng id", b1.Ok && b2.Ok && s.FeSees == 4 && b2.Value!.Replayed && b1.Value!.EntryIds.Order().SequenceEqual(b2.Value.EntryIds.Order()) && s.DayKcal == s.EntriesKcal, Fmt(s, (b1.Ok, b1.Error, b1.Value), (b2.Ok, b2.Error, b2.Value))); }

    // 6) hai batch KHÁC key song song, ngày mới (2 + 2 món)
    { var bad = new List<string>();
      for (var round = 0; round < 5; round++)
      {
          var u = Guid.NewGuid();
          var rs = await Task.WhenAll(Batch(u, d, "a" + round, foods[0], foods[1]), Batch(u, d, "b" + round, foods[2], foods[3]));
          var s = await State(u, d);
          if (s.FeSees != 4 || rs.Any(x => !x.Ok) || s.DayKcal != s.EntriesKcal) bad.Add($"vòng {round}: " + Fmt(s, rs.Select(x => (x.Ok, x.Error, (object?)x.Value)).ToArray()));
      }
      Check("6 Hai batch (2+2) khác key chạy song song cùng ngày mới (5 vòng) ⇒ đủ 4 entry, cả hai request OK", bad.Count == 0, bad.Count == 0 ? "5/5 vòng đủ 4" : $"{bad.Count}/5 vòng sai; " + bad[0]); }

    // 7) cùng một key dùng cho hai lô KHÁC nhau (FE tái dùng key): lô hai bị coi là replay ⇒ món của lô hai KHÔNG được lưu mà vẫn trả 200
    { var u = Guid.NewGuid(); var b1 = await Batch(u, d, "same", foods[0], foods[1]); var b2 = await Batch(u, d, "same", foods[2], foods[3]); var s = await State(u, d);
      Check("7 Tái dùng IdempotencyKey cho lô KHÁC (2 món rồi 2 món khác) ⇒ lô hai bị TỪ CHỐI rõ ràng (409 IDEMPOTENCY_KEY_REUSED), không được trả 200 mà bỏ im lặng lô hai; lô một còn nguyên", !b2.Ok && b2.Error.Contains("ConflictException") && s.FeSees == 2, $"batch2 ok={b2.Ok} [{b2.Error}]; replayed={b2.Value?.Replayed}; FE sees {s.FeSees} (lô một)"); }

    // 9) ĐƯỜNG THẬT: IMediator + AddApplicationServices (đúng đăng ký pipeline của API: Validation → Performance → FoodLogWriteLock → handler); 4 Send song song, mỗi Send một scope như một request HTTP
    if (useLock)
    {
        var cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection().Build();
        var sc = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        sc.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(cfg);
        sc.AddSingleton(TimeProvider.System);
        sc.AddSingleton(algo);
        sc.AddLogging();
        sc.AddScoped<ScopedUser>();
        sc.AddScoped<IUserContext>(p => p.GetRequiredService<ScopedUser>());
        sc.AddScoped<IUnitOfWork>(_ => new UnitOfWork(new ApplicationDbContext(opts)));
        sc.AddScoped<IUserClock>(p => new UserClock(p.GetRequiredService<IUnitOfWork>(), p.GetRequiredService<IUserContext>(), TimeProvider.System));
        sc.AddApplicationServices(cfg);
        using var provider = sc.BuildServiceProvider();
        var bad = new List<string>();
        for (var round = 0; round < 5; round++)
        {
            var u = Guid.NewGuid();
            var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(async i =>
            {
                using var scope = provider.CreateScope();
                scope.ServiceProvider.GetRequiredService<ScopedUser>().UserId = u;
                try { await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new AddFoodLogEntryCommand(d, MealType.Lunch, new TimeOnly(12, 0), foods[i], null, 100)); return (Ok: true, Error: ""); }
                catch (Exception e) { return (Ok: false, Error: $"{e.GetType().Name}: {(e.InnerException ?? e).Message[..Math.Min(80, (e.InnerException ?? e).Message.Length)]}"); }
            }));
            var s = await State(u, d);
            if (s.FeSees != 4 || rs.Any(x => !x.Ok) || s.DayKcal != s.EntriesKcal) bad.Add($"vòng {round}: ok {rs.Count(x => x.Ok)}/4 [{string.Join(" | ", rs.Where(x => !x.Ok).Select(x => x.Error).Distinct())}], FE sees {s.FeSees}, day kcal {s.DayKcal} vs entries {s.EntriesKcal}");
        }
        Check("9 Qua IMediator THẬT (pipeline đăng ký như API): 4 Send AddFoodLogEntry song song cùng ngày mới (5 vòng) ⇒ cả 4 OK, FE thấy đủ 4, tổng ngày khớp", bad.Count == 0, bad.Count == 0 ? "5/5 vòng đủ 4" : $"{bad.Count}/5 vòng sai; " + bad[0]);
    }

    // 8) hỗn hợp song song cùng ngày: 3 thêm + 1 lô 3 món + sửa gram + xoá một món có sẵn ⇒ tổng ngày khớp entry, mọi request OK
    { var bad = new List<string>();
      for (var round = 0; round < 5; round++)
      {
          var u = Guid.NewGuid(); var e1 = (await AddEntry(u, d, foods[8])).Value; var e2 = (await AddEntry(u, d, foods[9])).Value;
          var rs = await Task.WhenAll(
              AddEntry(u, d, foods[0]).ContinueWith(t => (t.Result.Ok, t.Result.Error, (object?)t.Result.Value)),
              AddEntry(u, d, foods[1]).ContinueWith(t => (t.Result.Ok, t.Result.Error, (object?)t.Result.Value)),
              AddEntry(u, d, foods[2]).ContinueWith(t => (t.Result.Ok, t.Result.Error, (object?)t.Result.Value)),
              Batch(u, d, "mix" + round, foods[3], foods[4], foods[5]).ContinueWith(t => (t.Result.Ok, t.Result.Error, (object?)t.Result.Value)),
              EditGrams(u, e1, 250).ContinueWith(t => (t.Result.Ok, t.Result.Error, (object?)t.Result.Value)),
              Delete(u, e2).ContinueWith(t => (t.Result.Ok, t.Result.Error, (object?)t.Result.Value)));
          var s = await State(u, d);
          if (s.FeSees != 7 || rs.Any(x => !x.Ok) || s.DayKcal != s.EntriesKcal) bad.Add($"vòng {round}: " + Fmt(s, rs.Select(x => (x.Ok, x.Error, x.Item3)).ToArray()));
      }
      Check("8 Hỗn hợp song song cùng ngày (3 thêm + lô 3 món + sửa gram + xoá 1 món, 5 vòng) ⇒ 7 entry (2 sẵn − 1 xoá + 3 + 3 = 7), mọi request OK, tổng ngày = tổng entry", bad.Count == 0, bad.Count == 0 ? "5/5 vòng đúng" : $"{bad.Count}/5 vòng sai; " + bad[0]); }

    // 10) Không có đường xoá mềm ÂM THẦM: sao chép (cả sang ngày tương lai), chuyển (BulkMove, cả sang ngày khác), xoá của ngày khác KHÔNG làm mất/xoá mềm entry nào ngoài phạm vi thao tác
    {
        var u = Guid.NewGuid(); var src = d; var future = today.AddDays(1);
        await AddEntry(u, src, foods[0]); await AddEntry(u, src, foods[1]); await AddEntry(u, src, foods[2]);
        async Task<(int Live, int Soft)> Count(DateOnly day)
        {
            using var c = new ApplicationDbContext(opts);
            var q = c.Set<FoodLogEntry>().IgnoreQueryFilters().Where(e => e.FoodLog.UserId == u && e.FoodLog.LogDate == day);
            return (await q.CountAsync(e => !e.IsDeleted), await q.CountAsync(e => e.IsDeleted));
        }
        var c1 = await Req(uow => new CopyFoodLogCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new CopyFoodLogCommand(src, today.AddDays(-5), null, null), default));
        var c2 = await Req(uow => new CopyFoodLogCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new CopyFoodLogCommand(src, future, null, null), default));
        var afterCopy = (Src: await Count(src), D5: await Count(today.AddDays(-5)), Fut: await Count(future));
        var movedIds = new List<Guid>();
        using (var c = new ApplicationDbContext(opts)) movedIds = await c.Set<FoodLogEntry>().Where(e => e.FoodLog.UserId == u && e.FoodLog.LogDate == today.AddDays(-5)).Select(e => e.Id).Take(2).ToListAsync();
        var mv = await Req(uow => new BulkMoveEntriesCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new BulkMoveEntriesCommand(movedIds, today.AddDays(-3), null, null), default));
        var afterMove = (D5: await Count(today.AddDays(-5)), D3: await Count(today.AddDays(-3)), Src: await Count(src), Fut: await Count(future));
        var clr = await Req(uow => new ClearDayLogCommandHandler(uow, new TestUser(u), TimeProvider.System, algo).Handle(new ClearDayLogCommand(future), default));
        var afterClear = (Fut: await Count(future), Src: await Count(src), D3: await Count(today.AddDays(-3)));
        Check("10 copy (kể cả sang NGÀY TƯƠNG LAI) / BulkMove / ClearDay: chỉ thao tác explicit mới tạo entry xoá mềm; entry ngày khác nguyên vẹn, entry chuyển KHÔNG bị xoá mềm",
            c1.Ok && c2.Ok && mv.Ok && clr.Ok && afterCopy is { Src: (3, 0), D5: (3, 0), Fut: (3, 0) } && afterMove is { D5: (1, 0), D3: (2, 0), Src: (3, 0), Fut: (3, 0) } && afterClear is { Fut: (0, 3), Src: (3, 0), D3: (2, 0) },
            $"copy {afterCopy}; move {afterMove}; clear {afterClear}; errors [{c1.Error}|{c2.Error}|{mv.Error}|{clr.Error}]");
    }
}
finally { await Drop(); }

Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAIL");
return failed == 0 ? 0 : 1;

sealed class ScopedUser : IUserContext
{
    public Guid UserId { get; set; }
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}

sealed record TestUser(Guid UserId) : IUserContext
{
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}
