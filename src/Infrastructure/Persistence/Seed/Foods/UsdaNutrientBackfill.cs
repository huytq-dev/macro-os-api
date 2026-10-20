using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Dir">Thư mục file JSON FDC đã giải nén (như import-usda-bulk).</param>
/// <param name="MaxRequests">Trần số request FDC cho món catalog đã gắn fdcId mà fdcId không nằm trong file local; 0 hoặc không có client = không gọi API.</param>
public sealed record UsdaBackfillOptions(bool Apply, string Dir, int BatchSize, int MaxRequests, int DelayMs);

/// <param name="Foods">Số món thuộc nhóm.</param>
/// <param name="FoodsToUpdate">Số món sẽ được thêm ít nhất một dòng chất.</param>
/// <param name="Rows">Số dòng FoodNutrient sẽ thêm.</param>
/// <param name="NotFound">Số món có fdcId nhưng không lấy được dữ liệu (không có trong file local và không gọi được API).</param>
public sealed record UsdaBackfillGroup(int Foods, int FoodsToUpdate, int Rows, int NotFound, IReadOnlyList<string> NotFoundSamples);

/// <param name="Suggestions">Tối đa 3 món FDC gần tên nhất trong file local ("fdcId mô tả"), để người duyệt gắn bằng <c>verify-usda --fdc key=fdcId</c>; KHÔNG tự ghi.</param>
public sealed record UsdaBackfillNoFdc(string Key, string Name, string Provider, int Nutrients, IReadOnlyList<string> Suggestions);

/// <param name="Foods">Số món tính độ phủ.</param>
/// <param name="Now">Số món hiện có ≥ 20 chất (4 macro + dòng FoodNutrient có số).</param>
/// <param name="After">Số món có ≥ 20 chất sau backfill.</param>
public sealed record UsdaBackfillCoverage(int Foods, int Now, int After)
{
    public double NowPercent => Foods == 0 ? 0 : 100.0 * Now / Foods;
    public double AfterPercent => Foods == 0 ? 0 : 100.0 * After / Foods;
}

public sealed record UsdaBackfillReport(
    IReadOnlyDictionary<string, int> ReadByDataset, UsdaBackfillGroup UsdaFoods, UsdaBackfillGroup CatalogFoods, int ApiRequests, int RowsFromApi,
    IReadOnlyList<UsdaBackfillNoFdc> NoFdc, UsdaBackfillCoverage CatalogCoverage, UsdaBackfillCoverage UsdaCoverage, int RowsInserted, TimeSpan Elapsed, bool Applied, string? ApiError,
    int MacroFixes = 0, IReadOnlyList<string>? MacroFixSamples = null);

/// <summary>Món USDA nhập bằng mapper cũ có macro (protein/carbs/fat) = 0 dù FDC có số (Foundation ghi fat ở 298, carbs ở 205.2…): sửa đúng ô đang bằng 0, không đổi ô khác.</summary>
internal sealed record UsdaMacroFix(Guid FoodId, string Name, decimal? Protein, decimal? Carbs, decimal? Fat);

/// <summary>
/// Bổ sung chất còn thiếu cho món đã có, KHÔNG đổi món/macro/dòng chất sẵn có (chỉ thêm dòng FoodNutrient chưa tồn tại, Measured):
/// (a) món USDA nhập về (ProviderCode = usda, ExternalFoodId = fdcId) lấy từ file FDC local, không gọi API;
/// (b) món catalog đã có nguồn USDA (FoodSourceRecord Provider = usda) lấy theo fdcId đó, thiếu trong file local mới gọi API (có trần request);
/// (c) món catalog không có FDC: chỉ liệt kê (kèm gợi ý gần nhất), không bao giờ bịa số.
/// Dùng chung <see cref="UsdaFoodMapper.MapNutrients"/> (số USDA, đơn vị, số thay thế của Foundation) với import-usda-bulk. Mặc định dry-run; ghi theo lô, idempotent (chạy lại không thêm gì).
/// </summary>
public static class UsdaNutrientBackfill
{
    private static readonly string[] CatalogProviders = [FoodData.SeedProvider, CanonicalCatalogData.VnProvider, CanonicalCatalogData.CondimentProvider];
    public const int MinNutrientsForCoverage = 20, ApiIdsPerRequest = 20;

    public static async Task<UsdaBackfillReport> RunAsync(ApplicationDbContext db, UsdaBackfillOptions o, IUsdaClient? usda, TimeProvider time, Action<string>? progress = null, CancellationToken ct = default)
    {
        var clock = Stopwatch.StartNew();
        db.ChangeTracker.Clear();
        // Chất mới của seed (vd 855 AA) phải có định nghĩa trước khi backfill; seed chỉ thêm dòng thiếu, idempotent, không đổi dòng đã có
        if (o.Apply) await NutrientData.SeedAsync(db, ct);

        // 1. Đọc file FDC → fdcId → món
        var readBy = new Dictionary<string, int>();
        var local = new Dictionary<int, UsdaFood>();
        foreach (var path in Directory.EnumerateFiles(o.Dir, "*.json", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            var code = UsdaBulkImporter.Detect(path);
            if (code is null) continue;
            progress?.Invoke($"Đọc {Path.GetFileName(path)} ({code})...");
            var foods = UsdaFoodParser.ParseDataset(await File.ReadAllBytesAsync(path, ct), out _);
            readBy[code] = readBy.GetValueOrDefault(code) + foods.Count;
            foreach (var f in foods) local.TryAdd(f.FdcId, f);
        }

        // 2. Định nghĩa chất có số USDA (trừ 4 macro nằm ở cột FoodItem) và hiện trạng DB
        var definitions = (await db.Set<NutrientDefinition>().Where(d => d.UsdaNumber != null && !NutrientSnapshot.FoodColumnCodes.Contains(d.Code))
                .Select(d => new { d.UsdaNumber, d.Id, d.Code, d.Unit }).ToListAsync(ct))
            .ToDictionary(d => d.UsdaNumber!, d => new UsdaFoodMapper.Definition(d.Id, d.Code, d.Unit));

        var foodsInDb = await db.Set<FoodItem>().Where(f => f.IsActive && f.Source != FoodSource.UserCreated && f.Source != FoodSource.QuickAdd)
            .Select(f => new { f.Id, f.Name, f.ProviderCode, f.ExternalFoodId, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g }).ToListAsync(ct);
        var usdaRecords = await db.Set<FoodSourceRecord>().Where(s => s.Provider == "usda" && s.ExternalId != null).Select(s => new { s.FoodItemId, s.ExternalId }).ToListAsync(ct);
        var recordFdc = usdaRecords.Where(r => int.TryParse(r.ExternalId, out _)).ToDictionary(r => r.FoodItemId, r => int.Parse(r.ExternalId!));

        // Dòng chất đã có (kể cả dòng không có số: chỉ mục duy nhất theo món + chất nên không thêm được dòng thứ hai)
        var existing = (await db.Set<FoodNutrient>().Select(n => new { n.FoodItemId, n.NutrientDefinitionId, Has = n.AmountPer100g != null }).ToListAsync(ct))
            .GroupBy(n => n.FoodItemId).ToDictionary(g => g.Key, g => (Defs: g.Select(n => n.NutrientDefinitionId).ToHashSet(), WithValue: g.Count(n => n.Has)));

        // 3. Phân nhóm
        var groupA = new List<(Guid Id, string Name, int FdcId)>();
        var groupB = new List<(Guid Id, string Name, int FdcId)>();
        var groupC = new List<(Guid Id, string Name, string Provider)>();
        var macroFixes = new List<UsdaMacroFix>();
        foreach (var f in foodsInDb)
        {
            if (f.ProviderCode == "usda")
            {
                if (!int.TryParse(f.ExternalFoodId, out var id)) continue;
                groupA.Add((f.Id, f.Name, id));
                if (local.TryGetValue(id, out var fdc))
                {
                    var m = UsdaCatalogMatcher.Macros(fdc);
                    decimal? Fix(decimal stored, decimal? usdaValue) => stored == 0 && usdaValue is > 0 ? Math.Round(usdaValue.Value, 4) : null;
                    var fix = new UsdaMacroFix(f.Id, f.Name, Fix(f.ProteinPer100g, m.Protein), Fix(f.CarbsPer100g, m.Carbs), Fix(f.FatPer100g, m.Fat));
                    if (fix.Protein is not null || fix.Carbs is not null || fix.Fat is not null) macroFixes.Add(fix);
                }
            }
            else if (f.ProviderCode is not null && CatalogProviders.Contains(f.ProviderCode))
            {
                if (recordFdc.TryGetValue(f.Id, out var id)) groupB.Add((f.Id, f.Name, id));
                else groupC.Add((f.Id, f.Name, f.ProviderCode));
            }
        }

        // 4. Món catalog đã gắn fdcId mà file local không có → gọi API (có trần), mỗi request tối đa 20 id
        var fetched = new Dictionary<int, UsdaFood>();
        var requests = 0;
        string? apiError = null;
        var missingIds = groupB.Select(g => g.FdcId).Where(id => !local.ContainsKey(id)).Distinct().Order().ToList();
        if (missingIds.Count > 0 && usda is not null && o.MaxRequests > 0)
        {
            foreach (var chunk in missingIds.Chunk(ApiIdsPerRequest))
            {
                if (requests >= o.MaxRequests) { apiError = $"hết trần {o.MaxRequests} request, chạy lại để tiếp tục"; break; }
                if (requests > 0 && o.DelayMs > 0) await Task.Delay(o.DelayMs, ct);
                requests++;
                try { foreach (var u in await usda.GetFoodsAsync(chunk, ct)) fetched[u.FdcId] = u; }
                catch (ExternalServiceException ex) { apiError = ex.Message; break; }
                progress?.Invoke($"FDC API: {requests} request, lấy {fetched.Count}/{missingIds.Count} món");
            }
        }

        // 5. Tính dòng cần thêm
        var adds = new List<(Guid FoodId, Guid DefinitionId, decimal Amount)>();
        var apiRows = 0;

        UsdaBackfillGroup Plan(List<(Guid Id, string Name, int FdcId)> group)
        {
            int toUpdate = 0, rows = 0;
            var notFound = new List<string>();
            foreach (var (id, name, fdcId) in group)
            {
                var viaApi = !local.TryGetValue(fdcId, out var u) && fetched.TryGetValue(fdcId, out u);
                if (u is null) { notFound.Add($"{name} (fdc {fdcId})"); continue; }
                var have = existing.TryGetValue(id, out var e) ? e.Defs : [];
                var fresh = UsdaFoodMapper.MapNutrients(u, definitions).Mapped.DistinctBy(m => m.DefinitionId).Where(m => !have.Contains(m.DefinitionId)).ToList();
                if (fresh.Count == 0) continue;
                toUpdate++;
                rows += fresh.Count;
                if (viaApi) apiRows += fresh.Count;
                foreach (var m in fresh) adds.Add((id, m.DefinitionId, m.Amount));
            }
            return new UsdaBackfillGroup(group.Count, toUpdate, rows, notFound.Count, notFound.Take(8).ToList());
        }
        var planA = Plan(groupA);
        var planB = Plan(groupB);

        // 6. Nhóm (c): không có FDC → chỉ liệt kê + gợi ý gần nhất trong file local (bỏ FNDDS: món chế biến dễ gợi ý nhầm)
        var pool = local.Values.Where(f => f.DataType != "Survey (FNDDS)").ToList();
        var noFdc = new List<UsdaBackfillNoFdc>();
        foreach (var (id, name, provider) in groupC)
        {
            var tokens = UsdaCatalogMatcher.Tokens(name);
            var near = tokens.Count == 0 ? [] : pool.Where(f => tokens.Any(t => f.Description.Contains(t, StringComparison.OrdinalIgnoreCase)));
            var suggestions = UsdaCatalogMatcher.Rank(name, near).Where(c => c.Mismatch is null).Take(3).Select(c => $"{c.Food.FdcId} {c.Food.Description}").ToList();
            noFdc.Add(new UsdaBackfillNoFdc(id.ToString(), name, provider, 4 + (existing.TryGetValue(id, out var e) ? e.WithValue : 0), suggestions));
        }

        // 7. Độ phủ ≥ 20 chất trước/sau (4 macro luôn có)
        var addedBy = adds.GroupBy(a => a.FoodId).ToDictionary(g => g.Key, g => g.Count());
        UsdaBackfillCoverage Coverage(IEnumerable<Guid> ids)
        {
            int total = 0, now = 0, after = 0;
            foreach (var id in ids)
            {
                total++;
                var n = 4 + (existing.TryGetValue(id, out var e) ? e.WithValue : 0);
                if (n >= MinNutrientsForCoverage) now++;
                if (n + addedBy.GetValueOrDefault(id) >= MinNutrientsForCoverage) after++;
            }
            return new UsdaBackfillCoverage(total, now, after);
        }
        var catalogCoverage = Coverage(groupB.Select(g => g.Id).Concat(groupC.Select(g => g.Id)));
        var usdaCoverage = Coverage(groupA.Select(g => g.Id));

        var inserted = 0;
        if (o.Apply && macroFixes.Count > 0) await FixMacrosAsync(db, macroFixes, o.BatchSize, progress, ct);
        if (o.Apply && adds.Count > 0) inserted = await InsertAsync(db, adds, o, time, progress, ct);
        else db.ChangeTracker.Clear();

        return new UsdaBackfillReport(readBy, planA, planB, requests, apiRows, noFdc, catalogCoverage, usdaCoverage, inserted, clock.Elapsed, o.Apply, apiError,
            macroFixes.Count, macroFixes.Take(8).Select(f => $"{f.Name} (P {f.Protein?.ToString("0.##") ?? "-"}, C {f.Carbs?.ToString("0.##") ?? "-"}, F {f.Fat?.ToString("0.##") ?? "-"})").ToList());
    }

    private static async Task FixMacrosAsync(ApplicationDbContext db, List<UsdaMacroFix> fixes, int batchSize, Action<string>? progress, CancellationToken ct)
    {
        foreach (var chunk in fixes.Chunk(batchSize))
        {
            var ids = chunk.Select(f => f.FoodId).ToList();
            var foods = await db.Set<FoodItem>().Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
            foreach (var fix in chunk)
            {
                var food = foods[fix.FoodId];
                if (fix.Protein is { } p) food.ProteinPer100g = p;
                if (fix.Carbs is { } c) food.CarbsPer100g = c;
                if (fix.Fat is { } f) food.FatPer100g = f;
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        progress?.Invoke($"Đã sửa macro bằng 0 của {fixes.Count} món USDA");
    }

    // Theo lô món (AutoDetectChanges tắt, Add qua DbSet, một SaveChanges/lô, rồi Clear); chạy lại sau lỗi thì dòng đã ghi nằm trong "existing" nên bị bỏ qua
    private static async Task<int> InsertAsync(ApplicationDbContext db, List<(Guid FoodId, Guid DefinitionId, decimal Amount)> adds, UsdaBackfillOptions o, TimeProvider time, Action<string>? progress, CancellationToken ct)
    {
        var run = new FoodImportRun { Id = Guid.NewGuid(), Provider = "usda-backfill", Query = "backfill-nutrients", Status = ImportRunStatus.Running, StartedAt = time.GetUtcNow() };
        var wasAutoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var inserted = 0;
        try
        {
            db.Set<FoodImportRun>().Add(run);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            var foodBatches = adds.GroupBy(a => a.FoodId).Chunk(o.BatchSize).ToList();
            for (var b = 0; b < foodBatches.Count; b++)
            {
                foreach (var a in foodBatches[b].SelectMany(g => g))
                    db.Set<FoodNutrient>().Add(new FoodNutrient { Id = Guid.NewGuid(), FoodItemId = a.FoodId, NutrientDefinitionId = a.DefinitionId, AmountPer100g = a.Amount, AmountStatus = NutrientAmountStatus.Measured });
                await db.SaveChangesAsync(ct);
                inserted += foodBatches[b].Sum(g => g.Count());
                db.ChangeTracker.Clear();
                progress?.Invoke($"Lô {b + 1}/{foodBatches.Count}: đã thêm {inserted}/{adds.Count} dòng chất");
            }
            await FinishRunAsync(db, run.Id, ImportRunStatus.Succeeded, inserted, null, time, ct);
        }
        catch (Exception ex)
        {
            await FinishRunAsync(db, run.Id, ImportRunStatus.Failed, inserted, ex.Message, time, ct);
            throw;
        }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = wasAutoDetect; }
        return inserted;
    }

    private static async Task FinishRunAsync(ApplicationDbContext db, Guid runId, ImportRunStatus status, int imported, string? error, TimeProvider time, CancellationToken ct)
    {
        try
        {
            db.ChangeTracker.Clear();
            var row = await db.Set<FoodImportRun>().FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (row is null) return;
            db.ChangeTracker.AutoDetectChangesEnabled = true;
            row.Status = status; row.Imported = imported; row.FinishedAt = time.GetUtcNow(); row.Error = error is null ? null : error.Length <= 500 ? error : error[..500];
            await db.SaveChangesAsync(ct);
        }
        catch (Exception) when (status == ImportRunStatus.Failed) { /* đang xử lý lỗi gốc: không che bằng lỗi ghi nhật ký */ }
    }

    /// <summary>CSV món catalog không có FDC: food_id,name,provider,nutrients,suggestions (người duyệt chọn rồi gắn bằng verify-usda --fdc).</summary>
    public static void WriteNoFdcCsv(string path, IReadOnlyList<UsdaBackfillNoFdc> rows)
    {
        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
        var sb = new StringBuilder("food_id,name,provider,nutrients,suggestions\n");
        foreach (var r in rows) sb.Append(r.Key).Append(',').Append(Q(r.Name)).Append(',').Append(r.Provider).Append(',').Append(r.Nutrients).Append(',').Append(Q(string.Join(" | ", r.Suggestions))).Append('\n');
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}
