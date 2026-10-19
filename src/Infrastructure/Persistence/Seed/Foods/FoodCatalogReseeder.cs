namespace Infrastructure;

/// <param name="References">Số bản ghi dữ liệu NGƯỜI DÙNG đang trỏ vào món catalog (tên bảng → số dòng); các món này KHÔNG bị xoá, chỉ đổi tên/khẩu phần/icon tại chỗ.</param>
public sealed record ReseedReport(
    int CatalogFoods, int FoodsRenamed, int CanonicalsRenamed, int LanguageFixed, int ServingsRenamed, int IconsChanged, int IconsStillNull,
    IReadOnlyList<string> NullIconSamples, int ViAliasesRemoved, int CanonicalNameViCleared, IReadOnlyDictionary<string, int> References, bool Applied);

/// <summary>
/// Chuyển danh mục hệ thống sang tiếng Anh TẠI CHỖ, KHÔNG xoá cứng món (nhật ký, recipe, yêu thích, kế hoạch, icon riêng, cache AI đều trỏ vào FoodItem bằng FK Restrict).
/// Phạm vi: món có ProviderCode seed / vn-fct / cond (món user tự tạo, QuickAdd, USDA/OFF không đổi tên). Việc làm: đổi tên + Language + tên khẩu phần sang tiếng Anh,
/// tính lại IconKey cho mọi món hệ thống (không đụng icon user chọn ở UserFoodIcon), nếu <see cref="CanonicalCatalogData.IncludeVietnamese"/> = false thì xoá alias Việt
/// của món catalog và NameVi của canonical; cuối cùng chạy lại các seeder (chỉ thêm món/alias còn thiếu). Một transaction; dry-run (apply = false) không ghi gì.
/// </summary>
public static class FoodCatalogReseeder
{
    private static readonly string[] CatalogProviders = [FoodData.SeedProvider, CanonicalCatalogData.VnProvider, CanonicalCatalogData.CondimentProvider];
    private const int BatchSize = 1000;

    public static async Task<ReseedReport> RunAsync(ApplicationDbContext db, bool apply, CancellationToken ct = default)
    {
        db.ChangeTracker.Clear();
        var vnEnglish = CanonicalCatalogData.VietnameseFoods.ToDictionary(v => v.Key, v => v.En);
        var condimentNames = CanonicalCatalogData.Condiments.ToDictionary(c => c.Key, c => c.Name);

        // 0. Đổi tên các thực phẩm chuẩn từng mang tên Việt (danh sách canonical được đưa vào prompt AI nên phải là tiếng Anh). Seeder tra canonical theo tên chuẩn hoá nên đổi trước khi seed lại.
        var canonicals = await db.Set<CanonicalFood>().ToListAsync(ct);
        var takenNorms = canonicals.Select(c => c.NameNormalized).ToHashSet();
        var canonicalsRenamed = 0;
        foreach (var c in canonicals.Where(c => FoodEnglish.CanonicalRenames.ContainsKey(c.Name)).ToList())
        {
            var newName = FoodEnglish.CanonicalRenames[c.Name];
            var newNorm = FoodNameNormalizer.Normalize(newName);
            if (takenNorms.Contains(newNorm)) continue; // đã có canonical tên mới (seed bản mới chạy trước): không gộp tự động
            takenNorms.Remove(c.NameNormalized); takenNorms.Add(newNorm);
            c.Name = newName; c.NameNormalized = newNorm; canonicalsRenamed++;
        }

        // 1. Đổi tên / Language / khẩu phần của món catalog
        var catalog = await db.Set<FoodItem>().IgnoreQueryFilters().Include(f => f.Servings)
            .Where(f => f.ProviderCode != null && CatalogProviders.Contains(f.ProviderCode)).ToListAsync(ct);
        int renamed = 0, language = 0, servings = 0;
        foreach (var f in catalog)
        {
            var target = f.ProviderCode switch
            {
                FoodData.SeedProvider when f.ExternalFoodId is not null && FoodEnglish.SeedNames.TryGetValue(f.ExternalFoodId, out var n) => n,
                CanonicalCatalogData.VnProvider when f.ExternalFoodId is not null && vnEnglish.TryGetValue(f.ExternalFoodId, out var n) => n,
                CanonicalCatalogData.CondimentProvider when f.ExternalFoodId is not null && condimentNames.TryGetValue(f.ExternalFoodId, out var n) => n,
                _ => f.Name,
            };
            if (target != f.Name) { f.Name = target; renamed++; }
            if (f.ProviderCode != CanonicalCatalogData.CondimentProvider && f.Language != "en-US") { f.Language = "en-US"; language++; }
            foreach (var s in f.Servings.Where(s => FoodEnglish.HasVietnamese(s.ServingName)))
            {
                var en = FoodEnglish.ServingName(s.ServingName);
                if (en != s.ServingName) { s.ServingName = en; servings++; }
            }
        }

        // 2. Tính lại IconKey cho mọi món hệ thống (món đã có icon cũ không được backfill nên vẫn giữ icon/emoji cũ); icon user chọn nằm ở UserFoodIcon nên không mất
        int iconsChanged = 0, iconsNull = 0;
        var nullSamples = new List<string>();
        for (var skip = 0; ; skip += BatchSize)
        {
            var rows = await db.Set<FoodItem>().IgnoreQueryFilters()
                .Where(f => f.Source != FoodSource.UserCreated && f.Source != FoodSource.QuickAdd)
                .OrderBy(f => f.Id).Skip(skip).Take(BatchSize).ToListAsync(ct);
            if (rows.Count == 0) break;
            foreach (var f in rows)
            {
                var key = IconKeyResolver.Resolve(f.Name, f.Category);
                if (key != f.IconKey) { f.IconKey = key; iconsChanged++; }
                if (key is null)
                {
                    iconsNull++;
                    if (nullSamples.Count < 40 && f.ProviderCode is not null && CatalogProviders.Contains(f.ProviderCode)) nullSamples.Add(f.Name);
                }
            }
        }

        // 3. Chỉ tiếng Anh: xoá alias Việt của món catalog + NameVi của canonical
        int aliasesRemoved = 0, nameViCleared = 0;
        if (!CanonicalCatalogData.IncludeVietnamese)
        {
            var viNorms = CanonicalCatalogData.VietnameseAliasNorms();
            var catalogIds = catalog.Select(f => f.Id).ToHashSet();
            var aliases = await db.Set<FoodAlias>().Where(a => viNorms.Contains(a.AliasNormalized)).ToListAsync(ct);
            var doomed = aliases.Where(a => catalogIds.Contains(a.FoodItemId)).ToList();
            db.Set<FoodAlias>().RemoveRange(doomed);
            aliasesRemoved = doomed.Count;
            foreach (var c in await db.Set<CanonicalFood>().Where(c => c.NameVi != null).ToListAsync(ct)) { c.NameVi = null; nameViCleared++; }
        }

        var references = await CountReferencesAsync(db, catalog.Select(f => f.Id).ToList(), ct);
        var report = new ReseedReport(catalog.Count, renamed, canonicalsRenamed, language, servings, iconsChanged, iconsNull, nullSamples, aliasesRemoved, nameViCleared, references, apply);
        if (!apply) { db.ChangeTracker.Clear(); return report; }

        // 4. Ghi một transaction, rồi chạy lại seeder để thêm món/alias còn thiếu (idempotent theo ExternalFoodId)
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await FoodData.SeedAsync(db, ct);
        await CanonicalCatalogData.SeedAsync(db, ct);
        await FoodIconBackfill.RunAsync(db, ct);
        await tx.CommitAsync(ct);
        return report;
    }

    /// <summary>Cho biết xoá cứng món catalog sẽ hỏng bao nhiêu dữ liệu người dùng (FK Restrict): số dòng mỗi bảng đang trỏ vào món catalog.</summary>
    private static async Task<IReadOnlyDictionary<string, int>> CountReferencesAsync(ApplicationDbContext db, List<Guid> ids, CancellationToken ct)
    {
        var set = ids.ToHashSet();
        async Task<int> Count(IQueryable<Guid?> q) => (await q.Where(x => x != null).Select(x => x!.Value).ToListAsync(ct)).Count(set.Contains);
        return new Dictionary<string, int>
        {
            ["FoodLogEntries"] = await Count(db.Set<FoodLogEntry>().IgnoreQueryFilters().Select(e => e.FoodItemId)),
            ["RecipeIngredients"] = await Count(db.Set<RecipeIngredient>().IgnoreQueryFilters().Select(e => (Guid?)e.FoodItemId)),
            ["FoodFavorites"] = await Count(db.Set<FoodFavorite>().IgnoreQueryFilters().Select(e => (Guid?)e.FoodItemId)),
            ["PlannedMeals"] = await Count(db.Set<PlannedMeal>().IgnoreQueryFilters().Select(e => e.FoodItemId)),
            ["MealTemplateItems"] = await Count(db.Set<MealTemplateItem>().IgnoreQueryFilters().Select(e => e.FoodItemId)),
            ["UserFoodIcons"] = await Count(db.Set<UserFoodIcon>().IgnoreQueryFilters().Select(e => (Guid?)e.FoodItemId)),
            ["AiIngredientCaches"] = await Count(db.Set<AiIngredientCache>().IgnoreQueryFilters().Select(e => e.FoodItemId)),
        };
    }
}
