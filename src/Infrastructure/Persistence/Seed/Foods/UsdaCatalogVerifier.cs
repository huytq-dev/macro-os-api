namespace Infrastructure;

/// <summary>
/// Đối chiếu món catalog (seed / vn-fct / cond) với USDA FoodData Central: tìm FDC theo tên (Foundation + SR Legacy; món Việt/chế biến thử thêm Survey FNDDS),
/// so 4 macro và các chất phụ đang có, in chênh lệch &gt; 5 %. Mặc định dry-run (không ghi gì). <c>apply</c>: với món khớp chắc ghi số USDA vào macro + chất phụ đang có,
/// IsVerified/Verified, Source = USDA và dòng nguồn (FoodSourceRecord: Provider usda, ExternalId = fdcId). Giữ nguyên Id, ProviderCode và ExternalFoodId
/// (seeder tra món theo (ProviderCode, ExternalFoodId) nên đổi hai cột này sẽ làm seeder thêm món trùng). Một transaction.
/// </summary>
public static class UsdaCatalogVerifier
{
    private static readonly string[] Providers = [FoodData.SeedProvider, CanonicalCatalogData.VnProvider, CanonicalCatalogData.CondimentProvider];
    // 10 ứng viên mỗi lần tìm: FDC xếp theo độ liên quan chữ nên món đúng ("Almonds" raw, "Milk, whole") thường không nằm trong 5 đầu
    private const int CandidatesPerSearch = 10, MaxConsecutiveErrors = 3, Retries = 2;

    public static async Task<UsdaVerifyReport> RunAsync(ApplicationDbContext db, IUsdaClient usda, UsdaVerifyOptions o, TimeProvider time, CancellationToken ct = default)
    {
        db.ChangeTracker.Clear();
        var started = time.GetUtcNow();

        var foods = (await db.Set<FoodItem>().Where(f => f.IsActive && f.ProviderCode != null && f.ExternalFoodId != null && Providers.Contains(f.ProviderCode)).ToListAsync(ct))
            .Where(f => o.Only is null || f.ProviderCode == o.Only)
            .OrderBy(f => f.ProviderCode == CanonicalCatalogData.CondimentProvider && !f.IsVerified ? 0 : f.ProviderCode == CanonicalCatalogData.VnProvider ? 1 : f.ProviderCode == FoodData.SeedProvider ? 2 : 3)
            .ThenBy(f => f.ExternalFoodId, StringComparer.Ordinal).ToList();
        var ids = foods.Select(f => f.Id).ToList();

        var definitions = (await db.Set<NutrientDefinition>().Where(d => d.UsdaNumber != null).ToListAsync(ct)).ToDictionary(d => d.Id);
        var ownNutrients = (await db.Set<FoodNutrient>().Where(n => ids.Contains(n.FoodItemId) && n.AmountPer100g != null).ToListAsync(ct))
            .Where(n => definitions.ContainsKey(n.NutrientDefinitionId) && !NutrientSnapshot.FoodColumnCodes.Contains(definitions[n.NutrientDefinitionId].Code))
            .ToLookup(n => n.FoodItemId);
        var records = (await db.Set<FoodSourceRecord>().Where(s => ids.Contains(s.FoodItemId)).ToListAsync(ct)).ToDictionary(s => s.FoodItemId);

        var requests = 0;
        var consecutiveErrors = 0;
        var items = new List<UsdaVerifyItem>();
        var plan = new List<(FoodItem Food, UsdaFood Usda, decimal Score, bool Forced)>();

        // Một lời gọi FDC có giãn cách; chỉ thử lại khi lỗi mạng/429/5xx (lỗi 4xx khác như 400 là request sai, thử lại vô ích); false = thất bại hẳn kèm lý do
        async Task<(bool Ok, T? Value, string? Error)> Call<T>(Func<Task<T>> call)
        {
            for (var attempt = 0; attempt <= Retries; attempt++)
            {
                if (requests > 0 && o.DelayMs > 0) await Task.Delay(o.DelayMs, ct);
                requests++;
                try { return (true, await call(), null); }
                catch (ExternalServiceException ex) when (attempt < Retries && IsRetryable(ex)) { await Task.Delay(o.RetryDelayMs * (attempt + 1), ct); }
                catch (ExternalServiceException ex) { return (false, default, ex.Message); }
            }
            return (false, default, null);
        }

        async Task<(bool Ok, IReadOnlyList<UsdaFood> Foods, string? Error)> SearchAsync(string query, string dataTypes)
        {
            var found = await Call(() => usda.SearchAsync(query, CandidatesPerSearch, ct, dataTypes));
            if (!found.Ok) return (false, [], found.Error);
            if (found.Value!.Count == 0) return (true, [], null);
            var detail = await Call(() => usda.GetFoodsAsync(found.Value, ct));
            return detail.Ok ? (true, detail.Value!, null) : (false, [], detail.Error);
        }

        foreach (var f in foods)
        {
            var key = f.ExternalFoodId!;
            IReadOnlyList<UsdaFood> pool = [];
            UsdaVerifyItem Item(string status, UsdaCandidate? pick = null, IReadOnlyList<UsdaDiff>? macro = null, IReadOnlyList<UsdaDiff>? nutrient = null, string? error = null)
                => new(key, f.Name, f.ProviderCode!, status, pick?.Food.FdcId, pick?.Food.Description, pick?.Food.DataType, pick?.Score ?? 0, macro ?? [], nutrient ?? [], false, error,
                    TopCandidates(f.Name, pool), f.CaloriesPer100g, f.ProteinPer100g, f.FatPer100g, f.CarbsPer100g);

            // Món đã Verified (số seed khớp USDA SR, hoặc đã xác minh từ FDC) bỏ qua để không ghi đè số đúng; chỉ --fdc mới ép kiểm lại
            if (f.IsVerified && !o.Fdc.ContainsKey(key)) { items.Add(Item(UsdaVerifyStatus.Already)); continue; }
            if (consecutiveErrors >= MaxConsecutiveErrors || requests + 2 > o.MaxRequests) { items.Add(Item(UsdaVerifyStatus.NotChecked)); continue; }

            UsdaCandidate? pick;
            var forced = o.Fdc.TryGetValue(key, out var fdcId);
            var query = UsdaCatalogMatcher.Query(f.Name);
            if (!forced && query.Length == 0) { items.Add(Item(UsdaVerifyStatus.None)); continue; } // tên rỗng sau khi bỏ ngoặc: không có gì để tìm
            bool ok;
            string? error = null;
            if (forced)
            {
                var one = await Call(() => usda.GetFoodsAsync([fdcId], ct));
                ok = one.Ok;
                error = one.Error;
                pool = one.Value ?? [];
                pick = one.Value?.FirstOrDefault() is { } only ? UsdaCatalogMatcher.Score(f.Name, only) with { Score = 1m } : null;
            }
            else
            {
                var reference = await SearchAsync(query, UsdaDataTypes.Reference);
                ok = reference.Ok;
                error = reference.Error;
                pool = reference.Foods;
                pick = UsdaCatalogMatcher.Pick(f.Name, reference.Foods);
                // Món chế biến/món Việt không có ở Foundation/SR: thử Survey (FNDDS) nếu cũng còn ngân sách request
                if (ok && pick?.Grade != UsdaMatchGrade.Match && (f.Category == FoodCategory.FastFood || f.ProviderCode == CanonicalCatalogData.VnProvider) && requests + 2 <= o.MaxRequests)
                {
                    var survey = await SearchAsync(query, UsdaDataTypes.Survey);
                    ok = survey.Ok;
                    error = survey.Error;
                    pool = reference.Foods.Concat(survey.Foods).ToList();
                    pick = UsdaCatalogMatcher.Pick(f.Name, pool);
                }
            }

            if (!ok) { consecutiveErrors++; items.Add(Item(UsdaVerifyStatus.Error, error: error)); continue; }
            consecutiveErrors = 0;
            if (pick is null) { items.Add(Item(UsdaVerifyStatus.None)); continue; }

            var macros = UsdaCatalogMatcher.Macros(pick.Food);
            var macroDiffs = UsdaCatalogMatcher.MacroDiffs(f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g, macros);
            var nutrientDiffs = UsdaCatalogMatcher.NutrientDiffs(
                ownNutrients[f.Id].Select(n => (definitions[n.NutrientDefinitionId].Code, definitions[n.NutrientDefinitionId].UsdaNumber, definitions[n.NutrientDefinitionId].Unit, n.AmountPer100g!.Value)), pick.Food);

            var status = forced || pick.Grade == UsdaMatchGrade.Match
                ? (!forced && UsdaCatalogMatcher.IsSuspicious(f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g, macros) ? UsdaVerifyStatus.Review : UsdaVerifyStatus.Match)
                : pick.Grade == UsdaMatchGrade.Weak ? UsdaVerifyStatus.Weak : UsdaVerifyStatus.None;
            items.Add(Item(status, pick, macroDiffs, nutrientDiffs));
            if (status == UsdaVerifyStatus.Match) plan.Add((f, pick.Food, pick.Score, forced));
        }

        if (!o.Apply)
        {
            db.ChangeTracker.Clear();
            return new UsdaVerifyReport(foods.Count, requests, items, false);
        }

        // Ghi một transaction: macro + chất phụ đang có theo số USDA, nguồn FDC, dấu Verified
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = time.GetUtcNow();
        var applied = new HashSet<string>();
        foreach (var (food, u, score, forced) in plan)
        {
            var m = UsdaCatalogMatcher.Macros(u);
            if (m.Kcal is { } k) food.CaloriesPer100g = Round(k);
            if (m.Protein is { } p) food.ProteinPer100g = Round(p);
            if (m.Carbs is { } c) food.CarbsPer100g = Round(c);
            if (m.Fat is { } fat) food.FatPer100g = Round(fat);
            food.IsVerified = true;
            food.QualityStatus = FoodQualityStatus.Verified;
            food.Source = FoodSource.USDA;
            food.VerifiedAt = now;
            food.SourceCapturedAt = now;
            food.Version++;

            foreach (var n in ownNutrients[food.Id])
            {
                var def = definitions[n.NutrientDefinitionId];
                if (u.Nutrients.FirstOrDefault(x => x.Number == def.UsdaNumber && SameUnit(x.Unit, def.Unit)) is not { } v) continue;
                n.AmountPer100g = Math.Round(v.Amount, 4);
                n.AmountStatus = NutrientAmountStatus.Measured;
            }

            if (food.CanonicalFoodId is { } canonicalId)
            {
                var method = forced ? "manual" : "usda-verify";
                var confidence = Math.Min(score, 1m);
                if (records.TryGetValue(food.Id, out var record)) { record.Provider = "usda"; record.ExternalId = u.FdcId.ToString(); record.MatchMethod = method; record.MatchConfidence = confidence; }
                else db.Set<FoodSourceRecord>().Add(new FoodSourceRecord
                {
                    Id = Guid.NewGuid(), CanonicalFoodId = canonicalId, FoodItemId = food.Id, Provider = "usda", ExternalId = u.FdcId.ToString(),
                    Priority = CanonicalLinker.Priority(food), MatchConfidence = confidence, MatchMethod = method,
                });
            }
            applied.Add(food.ExternalFoodId!);
        }

        var partial = items.Any(i => i.Status is UsdaVerifyStatus.Error or UsdaVerifyStatus.NotChecked);
        db.Set<FoodImportRun>().Add(new FoodImportRun
        {
            Id = Guid.NewGuid(), Provider = "usda-verify", Query = "verify-usda", Status = partial ? ImportRunStatus.Partial : ImportRunStatus.Succeeded,
            StartedAt = started, FinishedAt = now, Imported = applied.Count, Skipped = items.Count - applied.Count,
            Error = partial ? $"{items.Count(i => i.Status == UsdaVerifyStatus.Error)} lỗi gọi FDC, {items.Count(i => i.Status == UsdaVerifyStatus.NotChecked)} món chưa kiểm (hết trần request)." : null,
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new UsdaVerifyReport(foods.Count, requests, items.Select(i => applied.Contains(i.Key) ? i with { Applied = true } : i).ToList(), true);
    }

    private static List<UsdaVerifyCandidate> TopCandidates(string ourName, IEnumerable<UsdaFood> pool)
        => UsdaCatalogMatcher.Rank(ourName, pool).Take(3).Select(c =>
        {
            var m = UsdaCatalogMatcher.Macros(c.Food);
            return new UsdaVerifyCandidate(c.Food.FdcId, c.Food.Description, c.Food.DataType, c.Score, c.Mismatch, m.Kcal, m.Protein, m.Fat, m.Carbs);
        }).ToList();

    // Lỗi mạng (không có mã HTTP), 429 (hết hạn mức) và 5xx thử lại được; 400/401/403/404… là request/khoá sai nên thử lại chỉ tốn thời gian và hạn mức
    private static bool IsRetryable(ExternalServiceException ex)
    {
        var raw = ex.Errors is { } e && e.TryGetValue(UsdaClient.UpstreamStatusKey, out var v) ? v.FirstOrDefault() : null;
        return raw is null || (int.TryParse(raw, out var status) && (status == 429 || status >= 500));
    }

    private static decimal Round(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);

    private static bool SameUnit(string usda, string ours)
    {
        static string Norm(string u) => u.Trim().ToLowerInvariant() is "µg" or "μg" or "ug" ? "mcg" : u.Trim().ToLowerInvariant();
        return Norm(usda) == Norm(ours);
    }
}
