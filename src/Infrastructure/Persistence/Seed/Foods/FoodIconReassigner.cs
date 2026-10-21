using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <summary>
/// Chạy lại <see cref="IconKeyResolver"/> cho món hệ thống (catalog seed/vn-fct/cond + USDA nhập hàng loạt), KHÔNG đụng món user tạo (Source UserCreated/QuickAdd) và KHÔNG đụng icon user đã chọn
/// (bảng UserFoodIcons là bảng riêng, không bị sửa). Món nào resolver trả null thì IconKey về null (FE dùng emoji) thay vì giữ icon sai.
/// Món USDA thử tên gọn trước, không ra thì thử tên gốc USDA (alias en-US). Báo cáo: số món đổi theo cặp cũ→mới, số món null, và DANH SÁCH KHÁI NIỆM THIẾU ICON THẬT
/// (nhóm theo từ đầu của tên) để nhờ AI vẽ thêm. Mặc định dry-run; ghi theo lô, idempotent.
/// </summary>
public static class FoodIconReassigner
{
    public static async Task<FoodIconReassignReport> RunAsync(ApplicationDbContext db, FoodIconReassignOptions o, Action<string>? progress = null, CancellationToken ct = default)
    {
        var clock = Stopwatch.StartNew();
        db.ChangeTracker.Clear();

        var foods = await db.Set<FoodItem>().Where(f => f.IsActive && f.Source != FoodSource.UserCreated && f.Source != FoodSource.QuickAdd)
            .Select(f => new { f.Id, f.Name, f.Category, f.ProviderCode, f.ExternalFoodId, f.IconKey }).ToListAsync(ct);
        var userOverrides = await db.Set<UserFoodIcon>().Select(i => i.FoodItemId).Distinct().CountAsync(ct);

        // Tên gốc USDA (alias en-US) của món USDA: dự phòng khi tên gọn không ra icon
        var aliasByFood = (await db.Set<FoodAlias>().Where(a => a.FoodItem.ProviderCode == "usda" && a.Locale == "en-US").Select(a => new { a.FoodItemId, a.Alias }).ToListAsync(ct))
            .GroupBy(a => a.FoodItemId).ToDictionary(g => g.Key, g => g.First().Alias);

        var groups = new Dictionary<string, string>();
        if (o.Dir is not null)
            foreach (var path in Directory.EnumerateFiles(o.Dir, "*.json", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
            {
                if (UsdaBulkImporter.Detect(path) is null) continue;
                progress?.Invoke($"Đọc nhóm FDC từ {Path.GetFileName(path)}...");
                foreach (var f in UsdaFoodParser.ParseDataset(await File.ReadAllBytesAsync(path, ct), out _))
                    if (f.Category is { Length: > 0 } c) groups[f.FdcId.ToString()] = c;
            }

        var updates = new List<(Guid Id, string? Key)>();
        var changes = new Dictionary<(string, string), List<string>>();
        var concepts = new Dictionary<(string Concept, string Kind), List<string>>();
        int nowNull = 0, wasNull = 0;
        foreach (var f in foods)
        {
            if (f.IconKey is null) wasNull++;
            var group = f.ProviderCode == "usda" && f.ExternalFoodId is not null ? groups.GetValueOrDefault(f.ExternalFoodId) : null;
            var alias = aliasByFood.GetValueOrDefault(f.Id);
            // Tên gọn → tên gốc USDA → mới tới fallback theo nhóm món (nhóm món chỉ là chỗ dựa cuối, không được che tên gốc)
            var key = IconKeyResolver.Resolve(f.Name, f.Category, group, categoryFallback: false) ?? (alias is null ? null : IconKeyResolver.Resolve(alias, f.Category, group, categoryFallback: false))
                ?? IconKeyResolver.Resolve(f.Name, f.Category, group);
            if (key is null) nowNull++;

            // Khái niệm thiếu icon: không ra icon, hoặc chỉ ra icon chung của nhóm
            var named = IconKeyResolver.Resolve(f.Name, f.Category, null, categoryFallback: false) ?? (alias is null ? null : IconKeyResolver.Resolve(alias, f.Category, null, categoryFallback: false));
            var kind = key is null ? "none" : named is null ? "generic" : null;
            if (kind is not null)
            {
                var concept = Concept(f.Name);
                if (!concepts.TryGetValue((concept, kind), out var examples)) concepts[(concept, kind)] = examples = [];
                examples.Add(f.Name);
            }

            if (key == f.IconKey) continue;
            updates.Add((f.Id, key));
            var pair = (f.IconKey ?? "null", key ?? "null");
            if (!changes.TryGetValue(pair, out var list)) changes[pair] = list = [];
            list.Add(f.Name);
        }

        if (o.Apply && updates.Count > 0) await ApplyAsync(db, updates, o.BatchSize, progress, ct);
        else db.ChangeTracker.Clear();

        var changeList = changes.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key.Item1, StringComparer.Ordinal).Take(40)
            .Select(kv => new FoodIconChange(kv.Key.Item1, kv.Key.Item2, kv.Value.Count, kv.Value.Take(3).ToList())).ToList();
        var conceptList = concepts.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key.Concept, StringComparer.Ordinal)
            .Select(kv => new FoodIconConcept(kv.Key.Concept, kv.Key.Kind, kv.Value.Count, kv.Value.Take(3).ToList())).ToList();
        return new FoodIconReassignReport(foods.Count, updates.Count, nowNull, wasNull, changeList, conceptList, concepts.Count, userOverrides, clock.Elapsed, o.Apply);
    }

    // Từ đầu của tên (trước dấu phẩy), chữ thường: "Game meat, muskrat, raw" → "game meat"; tên gọn không dấu phẩy ("Rhubarb") giữ nguyên
    private static string Concept(string name) => name.Split(',')[0].Trim().ToLowerInvariant();

    private static async Task ApplyAsync(ApplicationDbContext db, List<(Guid Id, string? Key)> updates, int batchSize, Action<string>? progress, CancellationToken ct)
    {
        var done = 0;
        foreach (var chunk in updates.Chunk(batchSize))
        {
            var ids = chunk.Select(u => u.Id).ToList();
            var rows = await db.Set<FoodItem>().Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
            foreach (var (id, key) in chunk) rows[id].IconKey = key;
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            done += chunk.Length;
            progress?.Invoke($"Đã đổi icon {done}/{updates.Count} món");
        }
    }

    /// <summary>CSV khái niệm thiếu icon: concept,kind,count,examples (kind none = chưa có icon nào; generic = chỉ có icon chung của nhóm).</summary>
    public static void WriteConceptsCsv(string path, IReadOnlyList<FoodIconConcept> rows)
    {
        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
        var sb = new StringBuilder("concept,kind,count,examples\n");
        foreach (var r in rows) sb.Append(Q(r.Concept)).Append(',').Append(r.Kind).Append(',').Append(r.Count).Append(',').Append(Q(string.Join(" | ", r.Examples))).Append('\n');
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}
