using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Infrastructure;

/// <summary>
/// Sửa nhãn khẩu phần của món USDA nhập hàng loạt, GIỮ NGUYÊN Id khẩu phần (entry/recipe/favorite cũ trỏ vào vẫn đúng):
/// (1) tên toàn số ("1 90000", "1.90000": FNDDS "Quantity not specified" + mã khẩu phần 90000) → "1 serving", gram giữ nguyên;
/// (2) "RACC" → "serving"; (3) mã món bị ghép vào nhãn ("0.5 cup From 19211") → bỏ phần mã.
/// Trùng tên với khẩu phần đã có của món thì: chưa ai dùng → xoá, đang được tham chiếu → đổi theo gram. FDC không có mô tả nào tốt hơn cho các khẩu phần này nên không cần đọc lại file.
/// Món có khẩu phần mặc định là khẩu phần chung ("1 serving") thì chọn lại mặc định: cup, rồi khẩu phần đếm được, không thì "100 g".
/// Chỉ đụng món USDA; dry-run mặc định; ghi theo lô, idempotent.
/// </summary>
public static partial class UsdaServingFixer
{
    // Tên chỉ gồm số (có thể kèm dấu chấm) và tuỳ chọn một mã ≥ 4 chữ số: "1 90000", "1.90000", "0.5 10205", "2"
    [GeneratedRegex(@"^[\d.,]+(\s+\d{4,})?$")]
    private static partial Regex BadName();

    public static bool IsBadName(string name) => BadName().IsMatch(name.Trim());

    // "1 RACC" (Reference Amount Customarily Consumed): tên hợp lệ nhưng viết tắt vô nghĩa → "1 serving"
    [GeneratedRegex(@"\bRACC\b")]
    private static partial Regex Racc();

    // "0.5 cup From 19211": mã món USDA bị ghép vào nhãn
    [GeneratedRegex(@"\s*\bFrom\s+\d{4,}\b")]
    private static partial Regex FoodCode();

    // Khẩu phần chung "1 serving" / "2 serving": không dùng làm mặc định nếu có khẩu phần cụ thể hơn
    [GeneratedRegex(@"^[\d.,]+\s+serving$", RegexOptions.IgnoreCase)]
    private static partial Regex GenericServing();

    [GeneratedRegex(@"\b(piece|slice|medium|large|small|each|egg|fruit|tbsp|tablespoon|tsp|teaspoon|oz|patty|link|can|bar|stick|bottle|container|packet|pat|cookie|bun|roll|fillet|breast|thigh|drumstick|wing)s?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Countable();

    private enum Kind { Ok, Bad, Racc, Code }

    private static Kind Classify(string name) => IsBadName(name) ? Kind.Bad : Racc().IsMatch(name) ? Kind.Racc : FoodCode().IsMatch(name) ? Kind.Code : Kind.Ok;

    private sealed record Item(Guid Id, string Name, decimal Grams, bool IsDefault);

    private sealed record Plan(Guid FoodId, string FoodName, List<Guid> Delete, List<(Guid Id, string NewName)> Rename, bool ResetDefault, Guid? NewDefaultId, bool CreateHundred,
        int BadRenamed, int RaccCount, int CodeCount);

    public static async Task<UsdaServingFixReport> RunAsync(ApplicationDbContext db, UsdaServingFixOptions o, Action<string>? progress = null, CancellationToken ct = default)
    {
        var clock = Stopwatch.StartNew();
        db.ChangeTracker.Clear();

        var servings = await db.Set<FoodServing>().Where(s => s.FoodItem.ProviderCode == "usda")
            .Select(s => new { s.Id, s.FoodItemId, FoodName = s.FoodItem.Name, s.ServingName, s.WeightInGrams, s.IsDefault }).ToListAsync(ct);
        var usdaFoods = await db.Set<FoodItem>().CountAsync(f => f.ProviderCode == "usda", ct);

        // Khẩu phần đang được dùng (kể cả entry đã xoá mềm): không được xoá
        var referenced = new HashSet<Guid>();
        async Task Add(IQueryable<Guid?> ids) { foreach (var id in await ids.Where(i => i != null).Distinct().ToListAsync(ct)) referenced.Add(id!.Value); }
        await Add(db.Set<FoodLogEntry>().IgnoreQueryFilters().Select(e => e.FoodServingId));
        await Add(db.Set<FoodFavorite>().IgnoreQueryFilters().Select(e => e.FoodServingId));
        await Add(db.Set<MealTemplateItem>().IgnoreQueryFilters().Select(e => e.FoodServingId));
        await Add(db.Set<PlannedMeal>().IgnoreQueryFilters().Select(e => e.FoodServingId));
        foreach (var id in await db.Set<RecipeIngredient>().IgnoreQueryFilters().Select(e => e.FoodServingId).Distinct().ToListAsync(ct)) referenced.Add(id);

        var plans = new List<Plan>();
        foreach (var food in servings.GroupBy(s => s.FoodItemId))
        {
            var items = food.Select(s => new Item(s.Id, s.ServingName, s.WeightInGrams, s.IsDefault)).OrderBy(i => i.Name, StringComparer.Ordinal).ThenBy(i => i.Id).ToList();
            var changed = items.Where(i => Classify(i.Name) != Kind.Ok).ToList();
            if (changed.Count == 0) continue;

            var taken = items.Where(i => Classify(i.Name) == Kind.Ok).Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var final = new Dictionary<Guid, string?>(items.Where(i => Classify(i.Name) == Kind.Ok).Select(i => KeyValuePair.Create(i.Id, (string?)i.Name)));
            var delete = new List<Guid>();
            var rename = new List<(Guid, string)>();
            int bad = 0, racc = 0, code = 0;
            foreach (var i in changed)
            {
                var kind = Classify(i.Name);
                var target = kind switch
                {
                    Kind.Bad => "1 serving",
                    Kind.Racc => Racc().Replace(i.Name, "serving"),
                    _ => FoodCode().Replace(i.Name, "").Trim(),
                };
                if (target.Length > 0 && taken.Add(target))
                {
                    if (target != i.Name) rename.Add((i.Id, target));
                    final[i.Id] = target;
                }
                else if (!referenced.Contains(i.Id)) { delete.Add(i.Id); final[i.Id] = null; continue; }
                else
                {
                    var name = $"{i.Grams:0.##} g";
                    for (var n = 2; !taken.Add(name); n++) name = $"{i.Grams:0.##} g ({n})";
                    rename.Add((i.Id, name));
                    final[i.Id] = name;
                }
                if (kind == Kind.Bad) bad++; else if (kind == Kind.Racc) racc++; else code++;
            }

            // Mặc định là khẩu phần chung hoặc đã bị xoá → chọn lại: cup, rồi đếm được, không thì 100 g
            var current = items.FirstOrDefault(i => i.IsDefault);
            var reset = current is not null && changed.Contains(current) && (final[current.Id] is not { } now || GenericServing().IsMatch(now));
            Guid? newDefault = null;
            var createHundred = false;
            if (reset)
            {
                var candidates = items.Where(i => final[i.Id] is { } n && !GenericServing().IsMatch(n) && !n.Equals("100 g", StringComparison.OrdinalIgnoreCase) && i.Id != current!.Id).ToList();
                var pick = candidates.FirstOrDefault(i => final[i.Id]!.Contains("cup", StringComparison.OrdinalIgnoreCase)) ?? candidates.FirstOrDefault(i => Countable().IsMatch(final[i.Id]!));
                var hundred = items.FirstOrDefault(i => i.Grams == 100 && final[i.Id] is { } n && n.Equals("100 g", StringComparison.OrdinalIgnoreCase));
                newDefault = pick?.Id ?? hundred?.Id;
                createHundred = newDefault is null;
            }
            plans.Add(new Plan(food.Key, food.First().FoodName, delete, rename, reset, newDefault, createHundred, bad, racc, code));
        }

        if (o.Apply && plans.Count > 0) await ApplyAsync(db, plans, o.BatchSize, progress, ct);
        else db.ChangeTracker.Clear();

        var samples = plans.Take(8).Select(p => $"{p.FoodName} (xoá {p.Delete.Count}, đổi tên {p.Rename.Count}{(p.ResetDefault ? ", chọn lại mặc định" : "")})").ToList();
        return new UsdaServingFixReport(usdaFoods, plans.Count, plans.Sum(p => p.Delete.Count), plans.Sum(p => p.BadRenamed), plans.Count(p => p.ResetDefault), plans.Count(p => p.CreateHundred),
            samples, clock.Elapsed, o.Apply, plans.Sum(p => p.RaccCount), plans.Sum(p => p.CodeCount));
    }

    // Hai lượt SaveChanges mỗi lô: chỉ mục duy nhất lọc "một khẩu phần mặc định / món" nên bỏ mặc định cũ + xoá/đổi tên trước, đặt mặc định mới sau
    private static async Task ApplyAsync(ApplicationDbContext db, List<Plan> plans, int batchSize, Action<string>? progress, CancellationToken ct)
    {
        var done = 0;
        foreach (var chunk in plans.Chunk(batchSize))
        {
            var foodIds = chunk.Select(p => p.FoodId).ToList();
            var tracked = (await db.Set<FoodServing>().Where(s => foodIds.Contains(s.FoodItemId)).ToListAsync(ct)).ToLookup(s => s.FoodItemId);

            foreach (var plan in chunk)
            {
                var rows = tracked[plan.FoodId].ToList();
                foreach (var s in rows.Where(s => plan.Delete.Contains(s.Id))) db.Set<FoodServing>().Remove(s);
                foreach (var (id, name) in plan.Rename) rows.Single(s => s.Id == id).ServingName = name;
                if (plan.ResetDefault) foreach (var s in rows.Where(s => s.IsDefault)) s.IsDefault = false;
            }
            await db.SaveChangesAsync(ct);

            foreach (var plan in chunk.Where(p => p.ResetDefault))
            {
                if (plan.NewDefaultId is { } id) tracked[plan.FoodId].Single(s => s.Id == id).IsDefault = true;
                else db.Set<FoodServing>().Add(new FoodServing { Id = Guid.NewGuid(), FoodItemId = plan.FoodId, ServingName = "100 g", WeightInGrams = 100, UnitType = ServingUnitType.Mass, IsDefault = true });
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            done += chunk.Length;
            progress?.Invoke($"Đã sửa {done}/{plans.Count} món");
        }
    }
}
