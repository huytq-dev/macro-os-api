namespace Application;

public sealed class ResolveAiIngredientQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IAiMealAnalyzer _analyzer, IOptions<AiOptions> _options, TimeProvider _timeProvider,
    ILogger<ResolveAiIngredientQueryHandler> _logger)
    : IRequestHandler<ResolveAiIngredientQuery, AiResolvedIngredientModel>
{
    private const int Candidates = 40;

    private sealed record Candidate(
        Guid Id, string Name, FoodCategory Category, FoodSource Source, bool Verified, bool Mine, bool Rep,
        decimal Kcal, decimal Protein, decimal Fat, decimal Carbs, List<(Guid Id, string Name, decimal Weight, bool IsDefault)> Servings);

    public async Task<AiResolvedIngredientModel> Handle(ResolveAiIngredientQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var name = request.Name.Trim();
        var grams = NutritionRounding.Round(request.Grams, 1);

        var norm = FoodNameNormalizer.Normalize(name);
        var keys = norm.Length == 0 ? [] : new[] { norm, FoodNameNormalizer.Singular(norm) }.Distinct().ToList();
        var locale = FoodNameNormalizer.DetectLocale(name);

        // state AI/FE gửi; không gửi thì suy từ chính tên ("Boiled egg", "trứng chiên")
        var requestedState = FoodPreparationState.Normalize(request.State);
        var state = requestedState != FoodPreparationState.None ? requestedState : FoodPreparationState.Detect(name);
        var unit = AiIngredientResolution.NormalizeUnit(request.Unit);

        (Candidate Hit, string Source)? found = null;
        if (keys.Count > 0)
        {
            var cacheKeys = keys.Select(k => AiIngredientResolution.CacheKey(k, requestedState)).ToList();
            found = await FromCacheAsync(userId, cacheKeys, locale, ct) is { } cached ? (cached, AiIngredientResolution.SourceCache)
                : await FromCanonicalAsync(userId, keys, state, ct) ?? await BroadAsync(userId, name, keys[^1], state, ct);
        }

        if (found is { } f)
        {
            var hit = f.Hit;
            var servings = hit.Servings.Select(s => (s.Id, s.Name, s.Weight)).ToList();

            ServingChoice serving;
            if (unit is not null && AiIngredientResolution.ChooseByUnit(servings, unit, request.Quantity, grams) is { } byUnit)
            {
                if (byUnit.Corrected)
                    _logger.LogWarning("AI ingredient {Name}: {Quantity} {Unit} = {ServingGrams} g, AI estimated {Grams} g (off by more than {Tolerance:P0}); using the database serving",
                        name, byUnit.Quantity, byUnit.Label, byUnit.WeightGrams, grams, AiIngredientResolution.ServingTolerance);
                serving = new ServingChoice(byUnit.ServingId, byUnit.Quantity, byUnit.Label, byUnit.WeightGrams);
            }
            else
            {
                var chosen = NutritionCalculator.ChooseServing(grams, AiIngredientResolution.ForGramChoice(servings));
                serving = chosen with { UnitLabel = AiIngredientResolution.ServingLabel(chosen.UnitLabel) };
            }

            // Macro đúng như Food Log sẽ lưu khi FE log (foodItemId + servingId + quantity): cùng gram, cùng công thức, cùng làm tròn snapshot
            decimal? servingWeight = hit.Servings.Where(s => s.Id == serving.ServingId).Select(s => (decimal?)s.Weight).FirstOrDefault();
            var m = NutritionCalculator.SnapshotMacros(
                hit.Kcal, hit.Protein, hit.Carbs, hit.Fat, null, NutritionCalculator.Grams(serving.Quantity, servingWeight));
            // Icon hiệu lực của món: icon riêng của user thắng icon của món
            var iconKey = (await FoodIconSupport.OverridesAsync(_uow, userId, [hit.Id], ct)).GetValueOrDefault(hit.Id)
                ?? await _uow.Repository<FoodItem>().Query().Where(x => x.Id == hit.Id).Select(x => x.IconKey).FirstOrDefaultAsync(ct);
            return new AiResolvedIngredientModel(
                true, hit.Id, hit.Name, FoodEmoji.For(hit.Category), serving.ServingId, serving.Quantity, serving.UnitLabel, serving.WeightGrams,
                m.Calories, m.Protein, m.Fat, m.Carbs, false, iconKey, f.Source, AiIngredientResolution.ServingOptions(hit.Servings));
        }

        // Không có trong DB: nhờ AI ước lượng (đánh dấu Estimated); chưa cấu hình hoặc lỗi thì trả 0, không bịa số
        if (!string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            try
            {
                var e = AiPlateNormalizer.NormalizeEstimate(await _analyzer.EstimateIngredientAsync(name, grams, ct));
                return new AiResolvedIngredientModel(false, null, name, null, null, grams, "g", grams, e.Calories, e.Protein, e.Fat, e.Carbs, true, IconKeyResolver.Resolve(name),
                    AiIngredientResolution.SourceAi);
            }
            catch (ExternalServiceException ex)
            {
                _logger.LogWarning(ex, "AI ingredient estimate failed for {Name}", name);
            }
        }
        return new AiResolvedIngredientModel(false, null, name, null, null, grams, "g", grams, 0, 0, 0, 0, false, IconKeyResolver.Resolve(name), AiIngredientResolution.SourceNone);
    }

    // Món user được phép dùng: hệ thống, hoặc của chính user. Nguyên liệu AI là tên chung nên bỏ món có thương hiệu của người khác.
    private IQueryable<FoodItem> Visible(Guid userId)
        => _uow.Repository<FoodItem>().Query().Where(f =>
            f.IsActive && f.Source != FoodSource.QuickAdd
            && (f.Source != FoodSource.UserCreated || f.CreatedByUserId == userId)
            && (f.CreatedByUserId == userId || f.Brand == null || f.Brand == ""));

    private static async Task<List<Candidate>> ProjectAsync(IQueryable<FoodItem> q, Guid userId, CancellationToken ct)
        => (await q.Select(f => new
            {
                f.Id, f.Name, f.Category, f.Source, f.IsVerified, Mine = f.CreatedByUserId == userId, f.IsRepresentative,
                f.CaloriesPer100g, f.ProteinPer100g, f.FatPer100g, f.CarbsPer100g,
                Servings = f.Servings.Select(s => new { s.Id, s.ServingName, s.WeightInGrams, s.IsDefault }).ToList(),
            }).ToListAsync(ct))
            .Select(f => new Candidate(f.Id, f.Name, f.Category, f.Source, f.IsVerified, f.Mine, f.IsRepresentative, f.CaloriesPer100g, f.ProteinPer100g, f.FatPer100g, f.CarbsPer100g,
                f.Servings.Select(s => (s.Id, s.ServingName, s.WeightInGrams, s.IsDefault)).ToList()))
            .ToList();

    private async Task<Candidate?> RepresentativeAsync(Guid userId, Guid canonicalId, CancellationToken ct)
        => (await ProjectAsync(Visible(userId).Where(f => f.CanonicalFoodId == canonicalId && f.IsRepresentative), userId, ct)).FirstOrDefault();

    private async Task<Candidate?> FromCacheAsync(Guid userId, List<string> keys, string locale, CancellationToken ct)
    {
        var row = await _uow.Repository<AiIngredientCache>().Query(tracking: true)
            .Where(c => c.UserId == userId && keys.Contains(c.NormalizedName))
            .OrderByDescending(c => c.Locale == locale).ThenBy(c => c.Id)
            .FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var hit = row.FoodItemId is { } fid
            ? (await ProjectAsync(Visible(userId).Where(f => f.Id == fid), userId, ct)).FirstOrDefault()
            : row.CanonicalFoodId is { } cid ? await RepresentativeAsync(userId, cid, ct) : null;
        if (hit is null) return null; // món đã xoá/không còn dùng được: bỏ qua đệm, tìm lại

        row.HitCount++;
        row.LastUsedAt = _timeProvider.GetUtcNow();
        await _uow.SaveChangesAsync(ct);
        return hit;
    }

    // Thực phẩm chuẩn trước, rồi chọn biến thể trong nhóm đó: tên trùng hẳn → cùng state → dòng đại diện (xem AiIngredientResolution.PickFood)
    private async Task<(Candidate Hit, string Source)?> FromCanonicalAsync(Guid userId, List<string> keys, string state, CancellationToken ct)
    {
        var canonicalId = await _uow.Repository<FoodAlias>().Query()
                .Where(a => a.CanonicalFoodId != null && keys.Contains(a.AliasNormalized)).Select(a => a.CanonicalFoodId).FirstOrDefaultAsync(ct)
            ?? await _uow.Repository<CanonicalFood>().Query()
                .Where(c => c.Status == CanonicalFoodStatus.Active && keys.Contains(c.NameNormalized)).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        if (canonicalId is not { } id) return null;

        var items = await ProjectAsync(Visible(userId).Where(f => f.CanonicalFoodId == id), userId, ct);
        return AiIngredientResolution.PickFood(items, c => c.Name, c => c.Rep, keys, state) is { } pick ? (pick.Item, pick.Source) : null;
    }

    // Tìm rộng (chưa có canonical / dữ liệu cũ): mọi từ phải có trong tên hoặc alias, rồi xếp hạng bằng FoodMatchRanker (có state thì món cùng state đứng đầu)
    private async Task<(Candidate Hit, string Source)?> BroadAsync(Guid userId, string name, string queryNormalized, string state, CancellationToken ct)
    {
        var words = AiIngredientMatcher.Words(name);
        if (words.Count == 0) return null;

        var q = Visible(userId);
        foreach (var w in words)
        {
            var word = w;
            var nword = FoodNameNormalizer.Normalize(w);
            var aliasFoodIds = _uow.Repository<FoodAlias>().Query().Where(a => a.AliasNormalized.Contains(nword)).Select(a => a.FoodItemId);
            q = q.Where(f => f.Name.Contains(word) || aliasFoodIds.Contains(f.Id));
        }

        var rows = await ProjectAsync(q.OrderBy(f => f.Name.Length).Take(Candidates), userId, ct);
        if (rows.Count == 0) return null;

        var ids = rows.Select(r => r.Id).ToList();
        var aliases = (await _uow.Repository<FoodAlias>().Query().Where(a => ids.Contains(a.FoodItemId)).Select(a => new { a.FoodItemId, a.AliasNormalized }).ToListAsync(ct))
            .GroupBy(a => a.FoodItemId).ToDictionary(g => g.Key, g => g.Select(a => a.AliasNormalized).ToList());

        var ranked = FoodMatchRanker.Rank(rows, queryNormalized, r => r.Name,
                r => aliases.GetValueOrDefault(r.Id) ?? [], r => AiIngredientMatcher.SourcePriority(r.Source, r.Verified, r.Mine))
            .ToList();
        var pick = state != FoodPreparationState.None ? ranked.FirstOrDefault(r => FoodPreparationState.Detect(r.Name) == state) ?? ranked[0] : ranked[0];
        return (pick, AiIngredientResolution.SourceSearch);
    }
}
