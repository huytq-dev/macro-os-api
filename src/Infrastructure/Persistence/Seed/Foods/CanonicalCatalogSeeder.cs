namespace Infrastructure;

public static partial class CanonicalCatalogData
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        // --- 1. Món Việt (FoodItem Unverified) ---
        var haveVn = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.ProviderCode == VnProvider).Select(f => f.ExternalFoodId!).ToListAsync(ct)).ToHashSet();
        foreach (var v in VietnameseFoods.Where(v => !haveVn.Contains(v.Key)))
            db.Set<FoodItem>().Add(new FoodItem
            {
                // Hiển thị tiếng Anh: tên Việt (v.Vi) chỉ còn là alias/NameVi
                Id = Guid.NewGuid(), Name = v.En, Category = v.Cat, Source = FoodSource.VnFct, IsVerified = false, QualityStatus = FoodQualityStatus.Unverified,
                ProviderCode = VnProvider, ExternalFoodId = v.Key, Country = "VN", Language = "en-US", OriginalUnits = "per 100 g",
                IconKey = IconKeyResolver.Resolve(v.En, v.Cat),
                CaloriesPer100g = v.Kcal, ProteinPer100g = v.P, CarbsPer100g = v.C, FatPer100g = v.F,
                Servings =
                [
                    new FoodServing { Id = Guid.NewGuid(), ServingName = FoodEnglish.ServingName(v.Serving), WeightInGrams = v.Grams, UnitType = ServingUnitType.Household, IsDefault = true },
                    new FoodServing { Id = Guid.NewGuid(), ServingName = "100 g", WeightInGrams = 100, UnitType = ServingUnitType.Mass, IsDefault = false },
                ],
            });
        await db.SaveChangesAsync(ct);

        // --- 2. Canonical + đại diện + nguồn ---
        var items = await db.Set<FoodItem>().IgnoreQueryFilters()
            .Where(f => f.ProviderCode == FoodData.SeedProvider || f.ProviderCode == VnProvider).ToListAsync(ct);
        var itemByKey = items.ToDictionary(f => f.ExternalFoodId!);
        // Tên Việt gốc lấy từ dữ liệu seed (không đọc từ DB: món trong DB mang tên tiếng Anh sau khi đổi sang English)
        var seedNames = FoodData.VietnameseNames;
        var canonByNorm = (await db.Set<CanonicalFood>().ToListAsync(ct)).ToDictionary(c => c.NameNormalized);
        var repOf = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.IsRepresentative && f.CanonicalFoodId != null).Select(f => f.CanonicalFoodId!.Value).ToListAsync(ct)).ToHashSet();
        var haveSource = (await db.Set<FoodSourceRecord>().Select(s => s.FoodItemId).ToListAsync(ct)).ToHashSet();

        var entries = Entries(seedNames).ToList();
        var canonOf = new Dictionary<string, CanonicalFood>();
        foreach (var e in entries)
        {
            if (!itemByKey.TryGetValue(e.Key, out var item)) continue;
            var norm = FoodNameNormalizer.Normalize(e.En);
            if (!canonByNorm.TryGetValue(norm, out var canon))
            {
                canon = new CanonicalFood { Id = Guid.NewGuid(), Name = e.En, NameNormalized = norm, NameVi = e.Vi, Category = item.Category };
                db.Set<CanonicalFood>().Add(canon);
                canonByNorm[norm] = canon;
            }
            canonOf[e.Key] = canon;
            if (item.CanonicalFoodId is null)
            {
                item.CanonicalFoodId = canon.Id;
                item.IsRepresentative = repOf.Add(canon.Id);
            }
            if (haveSource.Add(item.Id))
                db.Set<FoodSourceRecord>().Add(new FoodSourceRecord
                {
                    Id = Guid.NewGuid(), CanonicalFoodId = canon.Id, FoodItemId = item.Id, Provider = CanonicalLinker.Provider(item), ExternalId = item.ExternalFoodId,
                    Priority = CanonicalLinker.Priority(item), MatchConfidence = 1m, MatchMethod = "seed",
                });
        }
        await db.SaveChangesAsync(ct);

        // --- 3. Alias (gắn vào canonical + dòng đại diện) ---
        var aliasRows = await db.Set<FoodAlias>().ToListAsync(ct);
        var owner = aliasRows.Where(a => a.CanonicalFoodId != null).GroupBy(a => a.AliasNormalized).ToDictionary(g => g.Key, g => g.First().CanonicalFoodId!.Value);
        var haveAlias = aliasRows.GroupBy(a => (a.FoodItemId, a.AliasNormalized)).ToDictionary(g => g.Key, g => g.First());
        foreach (var e in entries)
        {
            if (!canonOf.TryGetValue(e.Key, out var canon)) continue;
            var rep = items.FirstOrDefault(f => f.CanonicalFoodId == canon.Id && f.IsRepresentative);
            if (rep is null) continue;
            foreach (var alias in e.Aliases)
            {
                var norm = FoodNameNormalizer.Normalize(alias);
                if (norm.Length == 0 || (owner.TryGetValue(norm, out var o) && o != canon.Id)) continue;
                owner[norm] = canon.Id;
                if (haveAlias.TryGetValue((rep.Id, norm), out var existing))
                {
                    existing.CanonicalFoodId ??= canon.Id;
                    continue;
                }
                var row = new FoodAlias
                {
                    Id = Guid.NewGuid(), FoodItemId = rep.Id, CanonicalFoodId = canon.Id, Alias = alias.Trim(), AliasNormalized = norm,
                    Locale = FoodNameNormalizer.DetectLocale(alias),
                };
                db.Set<FoodAlias>().Add(row);
                haveAlias[(rep.Id, norm)] = row;
            }
        }
        await db.SaveChangesAsync(ct);

        await SeedCondimentsAsync(db, ct);
    }

    /// <summary>Đọc chuỗi khẩu phần "tên=gram|tên=gram" (mục đầu là mặc định) thành FoodServing.</summary>
    public static List<FoodServing> ParseServings(string servings)
    {
        var list = new List<FoodServing>();
        foreach (var part in servings.Split('|'))
        {
            var i = part.LastIndexOf('=');
            var name = part[..i].Trim();
            list.Add(new FoodServing
            {
                Id = Guid.NewGuid(), ServingName = name, WeightInGrams = decimal.Parse(part[(i + 1)..], System.Globalization.CultureInfo.InvariantCulture),
                UnitType = name == "1 g" ? ServingUnitType.Mass : ServingUnitType.Household, IsDefault = list.Count == 0,
            });
        }
        return list;
    }

    // Gia vị / dầu mỡ / đường / nước chấm: món riêng cho từng biến thể, gắn canonical, đại diện, alias, sodium. Idempotent theo ExternalFoodId.
    private static async Task SeedCondimentsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        // --- 4a. FoodItem ---
        var haveKeys = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.ProviderCode == CondimentProvider).Select(f => f.ExternalFoodId!).ToListAsync(ct)).ToHashSet();
        foreach (var c in Condiments.Where(c => !haveKeys.Contains(c.Key)))
        {
            var verified = CondimentVerifiedKeys.Contains(c.Key);
            db.Set<FoodItem>().Add(new FoodItem
            {
                Id = Guid.NewGuid(), Name = c.Name, Category = c.Cat, Source = FoodSource.System, IsVerified = verified,
                QualityStatus = verified ? FoodQualityStatus.Verified : FoodQualityStatus.Unverified,
                ProviderCode = CondimentProvider, ExternalFoodId = c.Key, Language = FoodNameNormalizer.DetectLocale(c.Name), OriginalUnits = "per 100 g",
                IconKey = IconKeyResolver.Resolve(c.Name, c.Cat),
                CaloriesPer100g = c.Kcal, ProteinPer100g = c.P, CarbsPer100g = c.C, FatPer100g = c.F,
                Servings = ParseServings(c.Servings),
            });
        }
        await db.SaveChangesAsync(ct);

        // --- 4b. Canonical + nguồn + đại diện ---
        var items = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.ProviderCode == CondimentProvider).ToListAsync(ct)).ToDictionary(f => f.ExternalFoodId!);
        var canonByNorm = (await db.Set<CanonicalFood>().ToListAsync(ct)).ToDictionary(c => c.NameNormalized);
        var haveSource = (await db.Set<FoodSourceRecord>().Select(s => s.FoodItemId).ToListAsync(ct)).ToHashSet();
        var currentRep = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.IsRepresentative && f.CanonicalFoodId != null)
            .Select(f => new { f.Id, Canon = f.CanonicalFoodId!.Value }).ToListAsync(ct)).ToDictionary(x => x.Canon, x => x.Id);

        var groups = Condiments.GroupBy(c => c.Canonical).ToList();
        var canonOf = new Dictionary<string, CanonicalFood>();
        foreach (var grp in groups)
        {
            var norm = FoodNameNormalizer.Normalize(grp.Key);
            var repEntry = grp.FirstOrDefault(c => c.Rep) ?? grp.First();
            if (!canonByNorm.TryGetValue(norm, out var canon))
            {
                canon = new CanonicalFood { Id = Guid.NewGuid(), Name = grp.Key, NameNormalized = norm, NameVi = IncludeVietnamese ? repEntry.Vi : null, Category = repEntry.Cat };
                db.Set<CanonicalFood>().Add(canon);
                canonByNorm[norm] = canon;
            }
            canonOf[grp.Key] = canon;
            foreach (var c in grp)
            {
                var item = items[c.Key];
                item.CanonicalFoodId ??= canon.Id;
                if (haveSource.Add(item.Id))
                    db.Set<FoodSourceRecord>().Add(new FoodSourceRecord
                    {
                        Id = Guid.NewGuid(), CanonicalFoodId = canon.Id, FoodItemId = item.Id, Provider = CanonicalLinker.Provider(item), ExternalId = item.ExternalFoodId,
                        Priority = CanonicalLinker.Priority(item), MatchConfidence = 1m, MatchMethod = "seed",
                    });
            }
        }
        await db.SaveChangesAsync(ct);

        // Đại diện: canonical chưa có thì món Rep (hoặc món đầu) làm đại diện; canonical đã có đại diện khác mà món Rep tường minh (Salt) thì đổi đại diện.
        // Index duy nhất lọc theo đại diện nên bỏ cờ cũ và lưu trước rồi mới đặt cờ mới.
        var demote = new List<FoodItem>();
        var promote = new List<FoodItem>();
        foreach (var grp in groups)
        {
            var canon = canonOf[grp.Key];
            var wanted = grp.FirstOrDefault(c => c.Rep);
            var item = items[(wanted ?? grp.First()).Key];
            if (!currentRep.TryGetValue(canon.Id, out var repId)) promote.Add(item);
            else if (wanted is not null && repId != item.Id)
            {
                demote.Add(await db.Set<FoodItem>().IgnoreQueryFilters().FirstAsync(f => f.Id == repId, ct));
                promote.Add(item);
            }
        }
        foreach (var f in demote) f.IsRepresentative = false;
        if (demote.Count > 0) await db.SaveChangesAsync(ct);
        foreach (var f in promote) f.IsRepresentative = true;
        await db.SaveChangesAsync(ct);

        // --- 4c. Alias (gắn canonical + dòng đại diện hiện tại) ---
        var repAfter = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.IsRepresentative && f.CanonicalFoodId != null)
            .Select(f => new { f.Id, Canon = f.CanonicalFoodId!.Value }).ToListAsync(ct)).ToDictionary(x => x.Canon, x => x.Id);
        var aliasRows = await db.Set<FoodAlias>().ToListAsync(ct);
        var owner = aliasRows.Where(a => a.CanonicalFoodId != null).GroupBy(a => a.AliasNormalized).ToDictionary(g => g.Key, g => g.First().CanonicalFoodId!.Value);
        var haveAlias = aliasRows.GroupBy(a => (a.FoodItemId, a.AliasNormalized)).ToDictionary(g => g.Key, g => g.First());
        foreach (var c in Condiments)
        {
            var canon = canonOf[c.Canonical];
            if (!repAfter.TryGetValue(canon.Id, out var repId)) continue;
            var names = Filter(c.Aliases.Split('|').Append(c.Name).Append(c.Vi ?? ""), [c.Vi]);
            if (c.Rep || items[c.Key].Id == repId) names = names.Append(c.Canonical);
            foreach (var alias in names)
            {
                var norm = FoodNameNormalizer.Normalize(alias);
                if (norm.Length == 0 || (owner.TryGetValue(norm, out var o) && o != canon.Id)) continue;
                owner[norm] = canon.Id;
                if (haveAlias.TryGetValue((repId, norm), out var existing))
                {
                    existing.CanonicalFoodId ??= canon.Id;
                    continue;
                }
                var row = new FoodAlias
                {
                    Id = Guid.NewGuid(), FoodItemId = repId, CanonicalFoodId = canon.Id, Alias = alias.Trim(), AliasNormalized = norm,
                    Locale = FoodNameNormalizer.DetectLocale(alias),
                };
                db.Set<FoodAlias>().Add(row);
                haveAlias[(repId, norm)] = row;
            }
        }
        await db.SaveChangesAsync(ct);

        // --- 4d. Natri (FoodNutrient sodium_mg): chỉ ghi khi có số; thiếu = Unknown (không ghi dòng) ---
        var sodiumId = await db.Set<NutrientDefinition>().Where(d => d.Code == "sodium_mg").Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct);
        if (sodiumId is null) return;
        var vnIds = (await db.Set<FoodItem>().IgnoreQueryFilters().Where(f => f.ProviderCode == VnProvider).Select(f => new { f.Id, Key = f.ExternalFoodId! }).ToListAsync(ct))
            .ToDictionary(f => f.Key, f => f.Id);
        var wantSodium = Condiments.Where(c => c.SodiumMg is not null)
                .Select(c => (items[c.Key].Id, c.SodiumMg!.Value, CondimentVerifiedKeys.Contains(c.Key)))
            .Concat(ExistingSodium.Where(e => vnIds.ContainsKey(e.Key)).Select(e => (vnIds[e.Key], e.Value, true)))
            .ToList();
        var haveSodium = (await db.Set<FoodNutrient>().Where(n => n.NutrientDefinitionId == sodiumId).Select(n => n.FoodItemId).ToListAsync(ct)).ToHashSet();
        foreach (var (foodId, amount, measured) in wantSodium.Where(w => !haveSodium.Contains(w.Item1)))
            db.Set<FoodNutrient>().Add(new FoodNutrient
            {
                Id = Guid.NewGuid(), FoodItemId = foodId, NutrientDefinitionId = sodiumId.Value, AmountPer100g = amount,
                AmountStatus = measured ? NutrientAmountStatus.Measured : NutrientAmountStatus.Estimated,
            });
        await db.SaveChangesAsync(ct);
    }
}
