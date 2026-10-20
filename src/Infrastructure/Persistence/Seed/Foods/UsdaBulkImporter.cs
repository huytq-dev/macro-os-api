using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Dir">Thư mục chứa file JSON đã giải nén của FDC (tìm đệ quy; nhận dạng theo khoá gốc FoundationFoods / SRLegacyFoods / SurveyFoods, không theo tên file).</param>
/// <param name="Only">foundation | sr | fndds (null = cả ba).</param>
/// <param name="AllNutrients">false = chỉ chất core (bộ hiển thị mặc định của trang Nutrition); true = cả chất mở rộng.</param>
public sealed record UsdaBulkOptions(bool Apply, string Dir, string? Only, int? Max, int BatchSize, bool AllNutrients);

public sealed record UsdaNoIconCategory(string Category, int Count, IReadOnlyList<string> Examples);

public sealed record UsdaBulkReport(
    IReadOnlyDictionary<string, int> ReadByDataset, IReadOnlyList<string> IgnoredFiles, IReadOnlyDictionary<UsdaSkipReason, int> Skipped, IReadOnlyDictionary<UsdaSkipReason, List<string>> SkipSamples,
    int ToAdd, int WithIcon, IReadOnlyList<UsdaNoIconCategory> NoIcon, IReadOnlyDictionary<string, int> ToAddByDataset, int Servings, int Nutrients, int Aliases, int Inserted, TimeSpan Elapsed, bool Applied)
{
    public int Read => ReadByDataset.Values.Sum();
    public double IconPercent => ToAdd == 0 ? 0 : 100.0 * WithIcon / ToAdd;
}

/// <summary>
/// Nhập lớp "common foods" (Foundation + SR Legacy + Survey FNDDS, KHÔNG Branded) từ file tải về của FDC vào catalog, không dùng API (không vướng 1000 request/giờ).
/// Tái dùng <see cref="UsdaFoodMapper"/> + <c>NutrientDefinition.UsdaNumber</c> như <c>ImportUsdaFoodsCommand</c>; idempotent theo (ProviderCode = usda, ExternalFoodId = fdcId).
/// Món nhập vào đứng riêng (không gom/tạo thực phẩm chuẩn mới: tên chuẩn đi vào prompt AI nên hàng nghìn canonical mới sẽ làm phình prompt); tên gọn hiển thị, tên gốc USDA là alias để tìm.
/// Mặc định dry-run (không ghi gì). Ghi theo lô (AutoDetectChanges tắt), mỗi lô một SaveChanges nên chạy lại sau lỗi sẽ tiếp tục (món đã nhập bị bỏ qua).
/// </summary>
public static class UsdaBulkImporter
{
    private static readonly string[] CatalogProviders = [FoodData.SeedProvider, CanonicalCatalogData.VnProvider, CanonicalCatalogData.CondimentProvider];

    private static readonly (string Root, string Code)[] Datasets = [("FoundationFoods", "foundation"), ("SRLegacyFoods", "sr"), ("SurveyFoods", "fndds")];

    public static async Task<UsdaBulkReport> RunAsync(ApplicationDbContext db, UsdaBulkOptions o, TimeProvider time, Action<string>? progress = null, CancellationToken ct = default)
    {
        var clock = Stopwatch.StartNew();
        db.ChangeTracker.Clear();

        // 1. Đọc file
        var readBy = new Dictionary<string, int>();
        var ignored = new List<string>();
        var all = new List<UsdaFood>();
        foreach (var path in Directory.EnumerateFiles(o.Dir, "*.json", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            var code = Detect(path);
            if (code is null) { ignored.Add(Path.GetFileName(path)); continue; }
            if (o.Only is not null && o.Only != code) continue;
            progress?.Invoke($"Đọc {Path.GetFileName(path)} ({code})...");
            var foods = UsdaFoodParser.ParseDataset(await File.ReadAllBytesAsync(path, ct), out _);
            readBy[code] = readBy.GetValueOrDefault(code) + foods.Count;
            all.AddRange(foods);
        }

        // 2. Cái gì đã có: tên chuẩn hoá của catalog/canonical/alias catalog/món USDA đã nhập, và fdcId đã nhập
        var catalogKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.Source != FoodSource.UserCreated && f.Source != FoodSource.QuickAdd).Select(f => f.Name).ToListAsync(ct))
            catalogKeys.Add(FoodNameNormalizer.Normalize(n));
        foreach (var n in await db.Set<CanonicalFood>().Select(c => c.NameNormalized).ToListAsync(ct)) catalogKeys.Add(n);
        foreach (var n in await db.Set<FoodAlias>().Where(a => a.FoodItem.ProviderCode != null && CatalogProviders.Contains(a.FoodItem.ProviderCode)).Select(a => a.AliasNormalized).ToListAsync(ct))
            catalogKeys.Add(n);
        var importedIds = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.ProviderCode == "usda" && f.ExternalFoodId != null).Select(f => f.ExternalFoodId!).ToListAsync(ct)).ToHashSet();

        var plan = UsdaCommonFoods.Plan(all, catalogKeys, importedIds, o.Max);

        // 3. Map bằng mapper chung (macro vào cột FoodItem, chất khác thành FoodNutrient), thêm alias tên gốc
        var definitions = (await db.Set<NutrientDefinition>().Where(d => d.UsdaNumber != null && (o.AllNutrients || d.IsCore)).Select(d => new { d.UsdaNumber, d.Id, d.Code, d.Unit }).ToListAsync(ct))
            .ToDictionary(d => d.UsdaNumber!, d => new UsdaFoodMapper.Definition(d.Id, d.Code, d.Unit));
        var now = time.GetUtcNow();
        var mapped = new List<(UsdaMappedFood Food, UsdaBulkCandidate Source, FoodAlias? Alias)>();
        var noIcon = new Dictionary<string, List<string>>();
        var skipped = plan.Skipped.ToDictionary(kv => kv.Key, kv => kv.Value);
        var datasetOf = new Dictionary<string, int>();
        foreach (var c in plan.Selected)
        {
            if (UsdaFoodMapper.Map(c.Food with { Description = c.Name }, definitions, now) is not { } m) { skipped[UsdaSkipReason.NoEnergy] = skipped.GetValueOrDefault(UsdaSkipReason.NoEnergy) + 1; continue; }
            // Icon: tên gọn → tên gốc (nhiều từ hơn) → null (báo cáo theo nhóm USDA)
            m.Food.IconKey ??= IconKeyResolver.Resolve(c.Food.Description, m.Food.Category, c.Food.Category);
            if (m.Food.IconKey is null)
            {
                var category = string.IsNullOrWhiteSpace(c.Food.Category) ? "(không có nhóm)" : c.Food.Category!;
                if (!noIcon.TryGetValue(category, out var list)) noIcon[category] = list = [];
                list.Add(c.Name);
            }
            m.Food.Language = "en-US";
            FoodAlias? alias = null;
            var originalKey = FoodNameNormalizer.Normalize(c.Food.Description);
            if (originalKey.Length > 0 && originalKey != c.Key)
                alias = new FoodAlias { Id = Guid.NewGuid(), FoodItemId = m.Food.Id, Locale = "en-US", Alias = Trim(c.Food.Description, 256), AliasNormalized = Trim(originalKey, 256) };
            mapped.Add((m, c, alias));
            datasetOf[c.Food.DataType] = datasetOf.GetValueOrDefault(c.Food.DataType) + 1;
        }

        var noIconCategories = noIcon.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new UsdaNoIconCategory(kv.Key, kv.Value.Count, kv.Value.Take(5).ToList())).ToList();
        var servings = mapped.Sum(m => m.Food.Food.Servings.Count);
        var nutrients = mapped.Sum(m => m.Food.Nutrients.Count);
        var aliases = mapped.Count(m => m.Alias is not null);

        int inserted = 0;
        if (o.Apply && mapped.Count > 0) inserted = await InsertAsync(db, mapped, o, now, time, progress, ct);
        else db.ChangeTracker.Clear();

        return new UsdaBulkReport(readBy, ignored, skipped, plan.Samples, mapped.Count, mapped.Count - noIcon.Values.Sum(l => l.Count), noIconCategories, datasetOf,
            servings, nutrients, aliases, inserted, clock.Elapsed, o.Apply);
    }

    // Từng lô: AutoDetectChanges tắt, Add cả đồ thị, một SaveChanges, rồi Clear tracker; lỗi giữa chừng thì các lô trước đã nằm trong DB và chạy lại sẽ bỏ qua chúng
    private static async Task<int> InsertAsync(ApplicationDbContext db, List<(UsdaMappedFood Food, UsdaBulkCandidate Source, FoodAlias? Alias)> mapped, UsdaBulkOptions o, DateTimeOffset started, TimeProvider time, Action<string>? progress, CancellationToken ct)
    {
        var run = new FoodImportRun { Id = Guid.NewGuid(), Provider = "usda-bulk", Query = $"import-usda-bulk {o.Only ?? "all"}", Status = ImportRunStatus.Running, StartedAt = started };
        var wasAutoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var inserted = 0;
        try
        {
            db.Set<FoodImportRun>().Add(run);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            var batches = (int)Math.Ceiling(mapped.Count / (double)o.BatchSize);
            for (var b = 0; b < batches; b++)
            {
                foreach (var (m, _, alias) in mapped.Skip(b * o.BatchSize).Take(o.BatchSize))
                {
                    db.Set<FoodItem>().Add(m.Food);
                    foreach (var n in m.Nutrients.DistinctBy(x => x.DefinitionId)) // hai số USDA cùng về một định nghĩa chất thì chỉ giữ một dòng (index duy nhất theo món + chất)
                        db.Set<FoodNutrient>().Add(new FoodNutrient { Id = Guid.NewGuid(), FoodItemId = m.Food.Id, NutrientDefinitionId = n.DefinitionId, AmountPer100g = n.Amount, AmountStatus = NutrientAmountStatus.Measured });
                    if (alias is not null) db.Set<FoodAlias>().Add(alias);
                }
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
                inserted = Math.Min((b + 1) * o.BatchSize, mapped.Count);
                progress?.Invoke($"Lô {b + 1}/{batches}: đã nhập {inserted}/{mapped.Count} món");
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
            row.Status = status; row.Imported = imported; row.FinishedAt = time.GetUtcNow(); row.Error = error is null ? null : Trim(error, 500);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception) when (status == ImportRunStatus.Failed) { /* đang xử lý lỗi gốc: không che bằng lỗi ghi nhật ký */ }
    }

    /// <summary>Nhận dạng dataset theo khoá gốc trong ~1 KB đầu file (không đọc cả file 200 MB); null nếu không phải file FDC hỗ trợ (vd Branded).</summary>
    public static string? Detect(string path)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[1024];
        var read = stream.Read(buffer, 0, buffer.Length);
        var head = Encoding.UTF8.GetString(buffer, 0, read);
        foreach (var (root, code) in Datasets)
            if (head.Contains($"\"{root}\"", StringComparison.Ordinal)) return code;
        return null;
    }

    /// <summary>CSV món không có icon theo nhóm USDA: category,count,examples (để bổ sung bảng icon).</summary>
    public static void WriteNoIconCsv(string path, IReadOnlyList<UsdaNoIconCategory> rows)
    {
        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
        var sb = new StringBuilder("category,count,examples\n");
        foreach (var r in rows) sb.Append(Q(r.Category)).Append(',').Append(r.Count).Append(',').Append(Q(string.Join(" | ", r.Examples))).Append('\n');
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
