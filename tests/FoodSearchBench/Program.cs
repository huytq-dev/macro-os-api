// Food Search bench: handler THẬT (grouped GET /foods/search và GET /foods cũ) trên LocalDB TẠM với danh mục thực tế:
// seed thật (FoodData + CanonicalCatalogData gồm món Việt có dấu + alias) + ~12k món kiểu USDA tổng hợp + lịch sử log của một user. Mỗi lần gọi một DbContext mới như request HTTP.
// In p50/p95 (ms), số dòng, kích thước JSON cho từng từ khoá; cuối cùng là các kiểm tra chất lượng xếp hạng (PASS/FAIL).
using System.Diagnostics;
using System.Text.Json;
using Application;
using Contract;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_SearchBench";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True;MultipleActiveResultSets=True";
var audit = new AuditSaveChangesInterceptor(new Microsoft.AspNetCore.Http.HttpContextAccessor(), TimeProvider.System);
var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).AddInterceptors(audit).Options;
var keep = Environment.GetEnvironmentVariable("BENCH_KEEP") == "1";
var runs = int.TryParse(Environment.GetEnvironmentVariable("BENCH_RUNS"), out var r) ? r : 15;
var user = Guid.Parse("11111111-1111-1111-1111-111111111111");
var failed = 0;
void Check(string name, bool ok, string detail) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name} — {detail}"); if (!ok) failed++; }

async Task Master(string sql)
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync();
}
async Task<bool> Exists()
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand(); cmd.CommandText = $"SELECT DB_ID('{db}')"; return await cmd.ExecuteScalarAsync() is not (null or DBNull);
}
async Task Drop() => await Master($"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");

async Task Seed()
{
    using var c = new ApplicationDbContext(opts);
    await c.Database.MigrateAsync();
    await c.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
    await FoodData.SeedAsync(c); await NutrientData.SeedAsync(c); await CanonicalCatalogData.SeedAsync(c);

    // ~12k món kiểu USDA (Source USDA, ProviderCode usda, không thương hiệu), mỗi món một khẩu phần
    string[] groups = ["Chicken", "Beef", "Pork", "Turkey", "Lamb", "Fish, salmon", "Fish, tuna", "Egg", "Milk", "Cheese", "Bread", "Rice", "Beans", "Apple", "Banana", "Tomato", "Potato", "Soup, tomato", "Oil, olive", "Yogurt", "Pasta", "Oats", "Spinach", "Carrots", "Orange"];
    string[] parts = ["breast", "thigh", "ground", "whole", "loin", "fillet", "raw", "canned", "frozen", "low fat", "whole grain", "organic"];
    string[] preps = ["raw", "cooked, roasted", "cooked, boiled", "cooked, grilled", "cooked, fried", "cooked, braised", "dried", "baked", "steamed", "stewed"];
    string[] extras = ["", "with salt", "without salt", "with skin", "without skin", "enriched", "unenriched"];
    var rng = new Random(42); var made = 0;
    foreach (var g in groups) foreach (var p in parts) foreach (var pr in preps) foreach (var ex in extras)
    {
        if (made >= 12000) break;
        var name = $"{g}, {p}, {pr}{(ex.Length > 0 ? ", " + ex : "")}";
        var id = Guid.NewGuid();
        c.Add(new FoodItem { Id = id, Name = name, Category = (FoodCategory)(made % 6), Source = FoodSource.USDA, ProviderCode = "usda", ExternalFoodId = "bench" + made, Country = "US", Language = "en-US", CaloriesPer100g = 50 + rng.Next(300), ProteinPer100g = rng.Next(30), CarbsPer100g = rng.Next(60), FatPer100g = rng.Next(30), IsVerified = true });
        c.Add(new FoodServing { Id = Guid.NewGuid(), FoodItemId = id, ServingName = "100 g", WeightInGrams = 100, IsDefault = true });
        made++;
        if (made % 2000 == 0) { await c.SaveChangesAsync(); c.ChangeTracker.Clear(); }
    }
    await c.SaveChangesAsync(); c.ChangeTracker.Clear();

    // lịch sử log của user: 120 entry trên 40 ngày, món ngẫu nhiên; thêm 6 lần log một món tuna cụ thể (kiểm tra món hay dùng được ưu tiên)
    var foodIds = await c.Set<FoodItem>().Select(f => f.Id).Take(3000).ToListAsync();
    var tuna = await c.Set<FoodItem>().Where(f => f.Name.StartsWith("Fish, tuna") && f.Name.Contains("canned")).OrderByDescending(f => f.Name).Select(f => f.Id).FirstAsync();
    // Món tiếng Việt CÓ DẤU: của user (Custom) và của hệ thống (VnFct), để kiểm tra "pho" tìm ra "Phở", "com" ra "Cơm" (danh mục seed chỉ có tên Anh/không dấu)
    foreach (var (vn, source, mine) in new[] { ("Phở bò tái", FoodSource.UserCreated, true), ("Bún chả Hà Nội", FoodSource.UserCreated, true), ("Cơm tấm sườn bì chả", FoodSource.UserCreated, true), ("Phở gà", FoodSource.VnFct, false), ("Bún bò Huế", FoodSource.VnFct, false), ("Cơm trắng", FoodSource.VnFct, false), ("Bánh mì thịt", FoodSource.VnFct, false), ("Đậu hũ chiên", FoodSource.VnFct, false) })
    {
        var id = Guid.NewGuid();
        c.Add(new FoodItem { Id = id, Name = vn, Category = FoodCategory.Other, Source = source, CreatedByUserId = mine ? user : null, Country = "VN", Language = "vi-VN", CaloriesPer100g = 150, ProteinPer100g = 8, CarbsPer100g = 20, FatPer100g = 4 });
        c.Add(new FoodServing { Id = Guid.NewGuid(), FoodItemId = id, ServingName = "1 phần", WeightInGrams = 250, IsDefault = true });
    }
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    for (var d = 0; d < 40; d++)
    {
        var logId = Guid.NewGuid();
        c.Add(new FoodLog { Id = logId, UserId = user, LogDate = today.AddDays(-d - 1) });
        for (var e = 0; e < 3; e++)
            c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = foodIds[rng.Next(foodIds.Count)], Quantity = 100, Calories = 100, CreatedAt = DateTimeOffset.UtcNow.AddDays(-d) });
        if (d < 6) c.Add(new FoodLogEntry { Id = Guid.NewGuid(), FoodLogId = logId, MealType = MealType.Dinner, FoodItemId = tuna, Quantity = 100, Calories = 100, CreatedAt = DateTimeOffset.UtcNow.AddDays(-d) });
    }
    await c.SaveChangesAsync();
}

var exists = await Exists();
if (!keep || !exists || Environment.GetEnvironmentVariable("BENCH_RESEED") == "1") { await Drop(); await Seed(); }
try
{
    var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    async Task<T> Req<T>(Func<UnitOfWork, Task<T>> f) { using var c = new ApplicationDbContext(opts); return await f(new UnitOfWork(c)); }
    Task<FoodSearchResponse> Grouped(string q, FoodSearchSection? s = null, int page = 1)
        => Req(uow => new SearchFoodsGroupedQueryHandler(uow, new TestUser(user), new NoOff(), new NoCache(), NullLogger<SearchFoodsGroupedQueryHandler>.Instance).Handle(new SearchFoodsGroupedQuery(q, s, page), default));
    Task<QueryResult<FoodItemModel>> Legacy(string q)
        => Req(uow => new SearchFoodsQueryHandler(uow, new TestUser(user)).Handle(new SearchFoodsQuery(q, null), default));

    async Task<(double P50, double P95, int Bytes)> Time<T>(Func<Task<T>> call)
    {
        for (var i = 0; i < 3; i++) await call();   // làm nóng (plan cache, JIT)
        var ms = new List<double>(); T last = default!;
        for (var i = 0; i < runs; i++) { var sw = Stopwatch.StartNew(); last = await call(); sw.Stop(); ms.Add(sw.Elapsed.TotalMilliseconds); }
        ms.Sort();
        return (ms[ms.Count / 2], ms[(int)Math.Ceiling(ms.Count * 0.95) - 1], JsonSerializer.SerializeToUtf8Bytes(last, web).Length);
    }

    // BENCH_COLLATE=1: thử các collation không phân biệt dấu trên tên món Việt có dấu (quyết định cách khớp "pho" ↔ "Phở", "dau hu" ↔ "Đậu hũ")
    if (Environment.GetEnvironmentVariable("BENCH_COLLATE") == "1")
    {
        using var c = new ApplicationDbContext(opts);
        Console.WriteLine("db collation: " + (await c.Database.SqlQueryRaw<string>("SELECT CONVERT(nvarchar(100), DATABASEPROPERTYEX('" + db + "','Collation')) AS [Value]").FirstAsync()));
        foreach (var col in new[] { "Latin1_General_100_CI_AI", "Latin1_General_CI_AI", "Vietnamese_100_CI_AI", "Vietnamese_CI_AI", "SQL_Latin1_General_CP1_CI_AI" })
            try
            {
                var counts = new List<string>();
                foreach (var pat in new[] { "%pho%", "%com%", "%dau hu%", "%bun%", "%banh mi%", "%do%" })
                    counts.Add($"{pat}={await c.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS [Value] FROM FoodItems WHERE Name COLLATE {col} LIKE N'{pat}'").FirstAsync()}");
                Console.WriteLine($"{col}: {string.Join(", ", counts)}");
            }
            catch (Exception e) { Console.WriteLine($"{col}: ERROR {e.InnerException?.Message ?? e.Message}"); }
        Console.WriteLine("default collation: " + string.Join(", ", await Task.WhenAll(new[] { "%pho%", "%com%", "%dau hu%" }.Select(async pat => $"{pat}={await c.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS [Value] FROM FoodItems WHERE Name LIKE N'{pat}'").FirstAsync()}"))));
        Console.WriteLine("Name column collation: " + (await c.Database.SqlQueryRaw<string>("SELECT collation_name AS [Value] FROM sys.columns WHERE object_id=OBJECT_ID('FoodItems') AND name='Name'").FirstAsync()));
    }

    // BENCH_TRACE="chicken,c" in từng câu SQL của một lần gọi (số lệnh, thời gian DB, số dòng đọc) để thấy N+1 và truy vấn chậm
    if (Environment.GetEnvironmentVariable("BENCH_TRACE") is { Length: > 0 } traceList)
        foreach (var tq in traceList.Split(','))
        {
            var tracer = new Tracer();
            var topts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).AddInterceptors(audit, tracer).Options;
            for (var w = 0; w < 2; w++) { tracer.Reset(); using var c = new ApplicationDbContext(topts); await new SearchFoodsGroupedQueryHandler(new UnitOfWork(c), new TestUser(user), new NoOff(), new NoCache(), NullLogger<SearchFoodsGroupedQueryHandler>.Instance).Handle(new SearchFoodsGroupedQuery(tq), default); }
            Console.WriteLine($"TRACE `{tq}`: {tracer.Count} lệnh SQL, tổng {tracer.TotalMs:F0} ms DB");
            foreach (var (sql, ms) in tracer.Items) Console.WriteLine($"   {ms,6:F1} ms  {sql}");
            if (Environment.GetEnvironmentVariable("BENCH_SQL") == "1") foreach (var full in tracer.Full) Console.WriteLine("-----\n" + full);
        }

    using (var c = new ApplicationDbContext(opts))
        Console.WriteLine($"catalog: FoodItems {await c.Set<FoodItem>().CountAsync()}, aliases {await c.Set<FoodAlias>().CountAsync()}, user entries {await c.Set<FoodLogEntry>().CountAsync(e => e.FoodLog.UserId == user)}; runs {runs}");
    Task<FoodSearchResponse> OldCall(string q, FoodSearchSection? s = null, int page = 1)
        => Req(uow => new OldSearchFoodsGroupedQueryHandler(uow, new TestUser(user), new NoOff(), new NoCache(), NullLogger<OldSearchFoodsGroupedQueryHandler>.Instance).Handle(new SearchFoodsGroupedQuery(q, s, page), default));
    { var sw = Stopwatch.StartNew(); await Grouped("chicken"); Console.WriteLine($"lần gọi ĐẦU TIÊN của tiến trình (nạp chỉ mục danh mục vào RAM, một lần mỗi {FoodCatalogIndexCache.Ttl.TotalMinutes:F0} phút): {sw.Elapsed.TotalMilliseconds:F0} ms; danh mục {(await FoodCatalogIndexCache.GetAsync(new UnitOfWork(new ApplicationDbContext(opts)), default)).Entries.Count} món"); }
    Console.WriteLine("| endpoint | query | NEW p50 ms | NEW p95 ms | OLD p50 ms | OLD p95 ms | NEW JSON bytes | detail |\n|---|---|---|---|---|---|---|---|");
    foreach (var q in new[] { "", "c", "ch", "chicken", "chicken breast", "tomato soup", "pho", "phở", "bo", "com", "tuna" })
    {
        var t = await Time(() => Grouped(q)); var o = await Time(() => OldCall(q)); var g = await Grouped(q);
        Console.WriteLine($"| GET /foods/search | `{q}` | {t.P50:F0} | {t.P95:F0} | {o.P50:F0} | {o.P95:F0} | {t.Bytes} | hist {g.History.TotalCount}, common {g.Common.TotalCount}, custom {g.Custom.TotalCount}, branded {g.Branded.TotalCount} |");
    }
    foreach (var q in new[] { "chicken", "c" })
    {
        var t = await Time(() => Grouped(q, FoodSearchSection.Common, 2)); var o = await Time(() => OldCall(q, FoodSearchSection.Common, 2)); var g = await Grouped(q, FoodSearchSection.Common, 2);
        Console.WriteLine($"| GET /foods/search?section=common&page=2 | `{q}` | {t.P50:F0} | {t.P95:F0} | {o.P50:F0} | {o.P95:F0} | {t.Bytes} | items {g.Common.Items.Count}/{g.Common.TotalCount} |");
    }
    foreach (var q in new[] { "chicken", "c", "phở", "" })
    {
        var t = await Time(() => Legacy(q)); var g = await Legacy(q);
        Console.WriteLine($"| GET /foods (legacy, SQL) | `{q}` | {t.P50:F0} | {t.P95:F0} | - | - | {t.Bytes} | items {g.Items.Count}/{g.TotalCount} |");
    }

    // ---- ngang bằng với bản cũ (oracle): cùng TỔNG từng nhóm và cùng tập món ở top (thứ tự được phép khác: từ-đầu-từ, món hay log lên trước) ----
    Task<FoodSearchResponse> Old(string q, FoodSearchSection? s = null, int page = 1)
        => Req(uow => new OldSearchFoodsGroupedQueryHandler(uow, new TestUser(user), new NoOff(), new NoCache(), NullLogger<OldSearchFoodsGroupedQueryHandler>.Instance).Handle(new SearchFoodsGroupedQuery(q, s, page), default));
    var parityBad = new List<string>();
    // (bỏ "c": một ký tự nay chỉ khớp đầu từ; bỏ "bo": nay khớp cả "Bò"/"Bún bò" có dấu: hai thay đổi CÓ CHỦ ĐÍCH)
    foreach (var q in new[] { "", "ch", "chicken", "chicken breast", "tomato soup", "egg", "tuna", "salt", "rice", "xyzzy" })
        foreach (var sec in new FoodSearchSection?[] { null, FoodSearchSection.Common })
        {
            var o = await Old(q, sec); var n = await Grouped(q, sec);
            foreach (var (name, og, ng) in new[] { ("history", o.History, n.History), ("common", o.Common, n.Common), ("custom", o.Custom, n.Custom), ("branded", o.Branded, n.Branded) })
                if (og.TotalCount != ng.TotalCount || og.Items.Count != ng.Items.Count) parityBad.Add($"`{q}` {sec?.ToString() ?? "overview"} {name}: tổng {og.TotalCount}→{ng.TotalCount}, dòng {og.Items.Count}→{ng.Items.Count}");
        }
    Check("P1 Ngang bản cũ: tổng và số dòng từng nhóm giống hệt trên 12 từ khoá × (tổng quan, section common)", parityBad.Count == 0, parityBad.Count == 0 ? "khớp hết" : string.Join(" ; ", parityBad.Take(6)));

    // ---- chất lượng xếp hạng ----
    var chicken = await Grouped("chicken");
    Check("Q1 'chicken': top 5 Common đều bắt đầu bằng 'Chicken' (khớp đầu từ trước khớp giữa)", chicken.Common.Items.Take(5).All(i => i.Name.StartsWith("Chicken", StringComparison.OrdinalIgnoreCase)), string.Join(" | ", chicken.Common.Items.Take(5).Select(i => i.Name)));
    async Task<bool> FindsAny(string q, string expectedSubstring, string label)
    {
        var g = await Grouped(q);
        var all = g.Common.Items.Concat(g.Custom.Items).Concat(g.Branded.Items).Concat(g.History.Items).Select(i => i.Name).ToList();
        var ok = all.Any(n => n.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase));
        Check($"{label}: '{q}' tìm ra món chứa '{expectedSubstring}' (không phân biệt dấu)", ok, ok ? "có" : "KHÔNG có; top: " + string.Join(" | ", all.Take(4)));
        return ok;
    }
    await FindsAny("pho", "Phở", "Q2");
    await FindsAny("phở", "Phở", "Q3");
    await FindsAny("pho", "pho", "Q3b");
    await FindsAny("com", "Cơm", "Q4");
    await FindsAny("bun", "Bún", "Q5");
    var tuna = await Grouped("tuna");
    Check("Q6 'tuna': món user hay log (6 lần gần đây) nằm trong top 3 của Common (ưu tiên món gần/hay dùng)", tuna.Common.Items.Take(3).Any(i => i.LastLoggedAt != null), string.Join(" | ", tuna.Common.Items.Take(3).Select(i => $"{i.Name} ({(i.LastLoggedAt != null ? "logged" : "-")})")));
    var empty = await Grouped("");
    Check("Q7 Không từ khoá: From History trả các món log gần đây, gần nhất trước, không rỗng", empty.History.Items.Count > 0 && empty.History.Items.Zip(empty.History.Items.Skip(1)).All(p => (p.First.LastLoggedAt ?? DateTimeOffset.MinValue) >= (p.Second.LastLoggedAt ?? DateTimeOffset.MinValue)), $"{empty.History.Items.Count} món");
    // ---- GET /foods (danh sách phẳng, endpoint FE đang dùng) ----
    Task<QueryResult<FoodItemModel>> Flat(string? q, FoodScope scope = FoodScope.All, FoodCategory? cat = null, int top = 10, int skip = 0)
        => Req(uow => new SearchFoodsQueryHandler(uow, new TestUser(user)).Handle(new SearchFoodsQuery(q, cat, scope, top, skip), default));
    var fChicken = await Flat("chicken"); var gChicken = await Grouped("chicken");
    Check("L1 GET /foods 'chicken': tổng = Common của /foods/search (844), mọi món bắt đầu bằng từ khoá đứng trước", fChicken.TotalCount == gChicken.Common.TotalCount && fChicken.Items.Take(5).All(i => i.Name.StartsWith("Chicken", StringComparison.OrdinalIgnoreCase)), $"tổng {fChicken.TotalCount}; {string.Join(" | ", fChicken.Items.Take(3).Select(i => i.Name))}");
    var fPho = await Flat("pho"); var fPhoVi = await Flat("phở");
    Check("L2 GET /foods không phân biệt dấu: 'pho' và 'phở' cùng ra 'Phở bò tái', 'Phở gà', 'Beef pho'", new[] { "Phở bò tái", "Phở gà", "Beef pho" }.All(n => fPho.Items.Any(i => i.Name == n) && fPhoVi.Items.Any(i => i.Name == n)) && fPho.TotalCount == fPhoVi.TotalCount, $"'pho' {fPho.TotalCount}: {string.Join(" | ", fPho.Items.Select(i => i.Name))}");
    var fMine = await Flat(null, FoodScope.Mine); var fSys = await Flat("pho", FoodScope.System); var fCat = await Flat("chicken", FoodScope.All, FoodCategory.Grains, 50);
    Check("L3 Phạm vi và category: Mine chỉ có món của tôi (IsMine), System không có món của tôi, category lọc đúng", fMine.TotalCount == 3 && fMine.Items.All(i => i.IsMine) && fSys.Items.All(i => !i.IsMine) && fCat.Items.All(i => i.Category == "Grains") && fCat.TotalCount > 0, $"mine {fMine.TotalCount}, system 'pho' {fSys.TotalCount}, grains chicken {fCat.TotalCount}");
    var fe = await Flat(null, top: 5); var fe2 = await Flat(null, top: 5, skip: 5);
    Check("L4 Không từ khoá: món log gần nhất lên đầu, phân trang liên tục (trang 2 không trùng trang 1), tổng = cả danh mục khớp", fe.Items.Count == 5 && fe.Items.All(i => i.LastLoggedAt != null) && !fe.Items.Select(i => i.Id).Intersect(fe2.Items.Select(i => i.Id)).Any() && fe.TotalCount > 12000, $"tổng {fe.TotalCount}; đầu: {string.Join(" | ", fe.Items.Take(2).Select(i => i.Name))}");
    var fTuna = await Flat("tuna");
    Check("L5 GET /foods 'tuna': món user hay log nằm trong top 3 (ưu tiên gần đây)", fTuna.Items.Take(3).Any(i => i.LastLoggedAt != null), string.Join(" | ", fTuna.Items.Take(3).Select(i => $"{i.Name} ({(i.LastLoggedAt != null ? "logged" : "-")})")));
    // phân trang kiểu infinite scroll của GET /foods: top + skip liên tiếp ghép lại đúng bằng một lần lấy lớn, không trùng, không sót, TotalCount ổn định
    var big = await Flat("chicken", top: 60); var pages = new List<FoodItemModel>(); var totalsSeen = new HashSet<int>();
    for (var sk = 0; sk < 60; sk += 20) { var pg = await Flat("chicken", top: 20, skip: sk); pages.AddRange(pg.Items); totalsSeen.Add(pg.TotalCount ?? -1); }
    var past = await Flat("chicken", top: 20, skip: 100000);
    Check("L6 GET /foods infinite scroll: 3 trang top=20 skip=0/20/40 ghép = lần lấy top=60 (cùng thứ tự, không trùng/sót), TotalCount ổn định 844; skip quá cuối ⇒ rỗng nhưng TotalCount giữ", pages.Select(p => p.Id).SequenceEqual(big.Items.Select(p => p.Id)) && pages.Select(p => p.Id).Distinct().Count() == 60 && totalsSeen.SequenceEqual([844]) && past.Items.Count == 0 && past.TotalCount == 844, $"ghép {pages.Count}, tổng {string.Join(",", totalsSeen)}, quá cuối {past.Items.Count}/{past.TotalCount}");
    var single = await Grouped("c", FoodSearchSection.Common);
    Check("Q9 Một ký tự chỉ khớp ĐẦU TỪ ('c' không kéo mọi món chứa chữ c giữa từ như 'Fish')", single.Common.Items.All(i => FoodCatalogIndex.Fold(i.Name).Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Any(w => w.StartsWith('c'))), $"{single.Common.TotalCount} món; mẫu: {string.Join(" | ", single.Common.Items.Take(3).Select(i => i.Name))}");
    // danh mục hệ thống đổi (món OFF mới từ mã vạch): Invalidate ⇒ lần tìm kế tiếp thấy ngay, không chờ hết TTL
    var newId = Guid.NewGuid(); var tag = "zz" + newId.ToString("N")[..8];
    using (var c = new ApplicationDbContext(opts))
    {
        c.Add(new FoodItem { Id = newId, Name = tag + " protein bar", Brand = "Acme", Category = FoodCategory.Snacks, Source = FoodSource.OpenFoodFacts, ProviderCode = "off", ExternalFoodId = "bench-off-" + newId.ToString("N"), CaloriesPer100g = 400, ProteinPer100g = 30, CarbsPer100g = 40, FatPer100g = 10 });
        await c.SaveChangesAsync();
    }
    var beforeInv = await Grouped(tag); FoodCatalogIndexCache.Invalidate(); var afterInv = await Grouped(tag);
    Check("Q10 Danh mục đổi: trước Invalidate chưa thấy món mới (chỉ mục RAM), sau Invalidate thấy ngay trong Branded (Acme)", beforeInv.Branded.TotalCount == 0 && afterInv.Branded.Items.Any(i => i.Id == newId && i.Brand == "Acme"), $"trước {beforeInv.Branded.TotalCount}, sau {afterInv.Branded.TotalCount}");
    var p1 = await Grouped("chicken", FoodSearchSection.Common, 1); var p2 = await Grouped("chicken", FoodSearchSection.Common, 2);
    Check("Q8 Phân trang section: trang 1 và 2 không trùng món, tổng khớp, trang đầy 20", p1.Common.Items.Count == 20 && p2.Common.Items.Count == 20 && !p1.Common.Items.Select(i => i.Id).Intersect(p2.Common.Items.Select(i => i.Id)).Any() && p1.Common.TotalCount == p2.Common.TotalCount, $"{p1.Common.Items.Count}/{p2.Common.Items.Count} tổng {p1.Common.TotalCount}");
}
finally { if (!keep) await Drop(); }

Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAIL");
return failed == 0 ? 0 : 1;

sealed class Tracer : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    public List<(string Sql, double Ms)> Items { get; } = [];
    public List<string> Full { get; } = [];
    public int Count => Items.Count;
    public double TotalMs => Items.Sum(i => i.Ms);
    public void Reset() { Items.Clear(); Full.Clear(); }
    public override ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData, System.Data.Common.DbDataReader result, CancellationToken ct = default)
    {
        var flat = string.Join(' ', command.CommandText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        Items.Add((flat[..Math.Min(150, flat.Length)], eventData.Duration.TotalMilliseconds));
        Full.Add(flat);
        return base.ReaderExecutedAsync(command, eventData, result, ct);
    }
}
sealed class NoCache : IRedisCacheService
{
    public Task SetRecordAsync<T>(string key, T value, TimeSpan? expiry = null) => Task.CompletedTask;
    public Task<T?> GetRecordAsync<T>(string key) => Task.FromResult<T?>(default);
    public Task RemoveRecordAsync(string key) => Task.CompletedTask;
    public Task RemoveManyAsync(params string[] keys) => Task.CompletedTask;
}
sealed class NoOff : IOpenFoodFactsClient
{
    public Task<OffProduct?> FindByBarcodeAsync(string code, CancellationToken ct) => Task.FromResult<OffProduct?>(null);
    public Task<OffSearchResult> SearchByNameAsync(string query, int page, int pageSize, CancellationToken ct) => throw new ExternalServiceException("bench: OFF disabled");
}
sealed record TestUser(Guid UserId) : IUserContext
{
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}
