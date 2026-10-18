namespace Application;

/// <summary>
/// Pha 2 của AI Plate: tìm một nguyên liệu (tên tiếng Anh hoặc Việt + gram) trong DB thực phẩm và tính macro từ món khớp.
/// Thứ tự: bộ nhớ đệm của user (món đã xác nhận) → dòng đại diện của thực phẩm chuẩn (qua alias/tên chuẩn hoá) → tìm rộng có xếp hạng.
/// Không khớp → AI ước lượng (Estimated=true) hoặc 0 (không bịa số). Không ghi nhật ký (chỉ cập nhật HitCount của bộ nhớ đệm khi trúng).
/// </summary>
public sealed record ResolveAiIngredientQuery(string Name, decimal Grams) : IRequest<AiResolvedIngredientModel>;

public sealed class ResolveAiIngredientQueryValidator : AbstractValidator<ResolveAiIngredientQuery>
{
    public ResolveAiIngredientQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Grams).GreaterThan(0).LessThanOrEqualTo(5000);
    }
}

/// <summary>Khớp tên nguyên liệu AI với món trong DB: tách từ và xếp hạng mức khớp. Chọn khẩu phần và tính macro do <see cref="NutritionCalculator"/> lo.</summary>
public static class AiIngredientMatcher
{
    /// <summary>Từ khoá tìm kiếm: chữ thường, bỏ dấu câu, số ít, tối đa 4 từ.</summary>
    public static IReadOnlyList<string> Words(string name)
        => Tokens(name).Select(Singular).Distinct().Take(4).ToList();

    // ponytail: bỏ "s" cuối cho tên tiếng Anh (Eggs → Egg); từ bất quy tắc (leaves, potatoes) để USDA alias lo
    private static string Singular(string w) => w.Length > 3 && w.EndsWith('s') && !w.EndsWith("ss") ? w[..^1] : w;

    private static IEnumerable<string> Tokens(string s)
        => new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>0 trùng hẳn · 1 bắt đầu bằng · 2 chứa nguyên từ · 3 chứa chuỗi · 4 chỉ khớp theo từng từ/alias.</summary>
    public static int Tier(string foodName, string term)
    {
        var n = foodName.ToLowerInvariant();
        var t = string.Join(' ', Words(term));
        if (n == t) return 0;
        if (n.StartsWith(t)) return 1;
        if ((" " + string.Join(' ', Tokens(n)) + " ").Contains(" " + t + " ")) return 2;
        return n.Contains(t) ? 3 : 4;
    }

    /// <summary>Thứ tự ưu tiên nguồn: hệ thống/USDA đã xác minh → hệ thống/USDA → của user → Open Food Facts.</summary>
    public static int SourcePriority(FoodSource source, bool verified, bool mine) => source switch
    {
        FoodSource.System or FoodSource.USDA => verified ? 0 : 1,
        FoodSource.UserCreated when mine => 2,
        FoodSource.OpenFoodFacts => 3,
        _ => 4,
    };
}


public sealed class ResolveAiIngredientQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IAiMealAnalyzer _analyzer, IOptions<AiOptions> _options, TimeProvider _timeProvider,
    ILogger<ResolveAiIngredientQueryHandler> _logger)
    : IRequestHandler<ResolveAiIngredientQuery, AiResolvedIngredientModel>
{
    private const int Candidates = 40;

    private sealed record Candidate(
        Guid Id, string Name, FoodCategory Category, FoodSource Source, bool Verified, bool Mine,
        decimal Kcal, decimal Protein, decimal Fat, decimal Carbs, List<(Guid Id, string Name, decimal Weight)> Servings);

    public async Task<AiResolvedIngredientModel> Handle(ResolveAiIngredientQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var name = request.Name.Trim();
        var grams = NutritionRounding.Round(request.Grams, 1);

        var norm = FoodNameNormalizer.Normalize(name);
        var keys = norm.Length == 0 ? [] : new[] { norm, FoodNameNormalizer.Singular(norm) }.Distinct().ToList();
        var locale = FoodNameNormalizer.DetectLocale(name);

        Candidate? hit = null;
        if (keys.Count > 0)
            hit = await FromCacheAsync(userId, keys, locale, ct) ?? await FromCanonicalAsync(userId, keys, ct) ?? await BroadAsync(userId, name, keys[^1], ct);

        if (hit is not null)
        {
            var serving = NutritionCalculator.ChooseServing(grams, hit.Servings.Select(s => (s.Id, s.Name, s.Weight)).ToList());
            // Macro đúng như Food Log sẽ lưu khi FE log (foodItemId + servingId + quantity): cùng gram, cùng công thức, cùng làm tròn snapshot
            decimal? servingWeight = hit.Servings.Where(s => s.Id == serving.ServingId).Select(s => (decimal?)s.Weight).FirstOrDefault();
            var m = NutritionCalculator.SnapshotMacros(
                hit.Kcal, hit.Protein, hit.Carbs, hit.Fat, null, NutritionCalculator.Grams(serving.Quantity, servingWeight));
            // Icon hiệu lực của món: icon riêng của user thắng icon của món
            var iconKey = (await FoodIconSupport.OverridesAsync(_uow, userId, [hit.Id], ct)).GetValueOrDefault(hit.Id)
                ?? await _uow.Repository<FoodItem>().Query().Where(f => f.Id == hit.Id).Select(f => f.IconKey).FirstOrDefaultAsync(ct);
            return new AiResolvedIngredientModel(
                true, hit.Id, hit.Name, FoodEmoji.For(hit.Category), serving.ServingId, serving.Quantity, serving.UnitLabel, serving.WeightGrams,
                m.Calories, m.Protein, m.Fat, m.Carbs, false, iconKey);
        }

        // Không có trong DB: nhờ AI ước lượng (đánh dấu Estimated); chưa cấu hình hoặc lỗi thì trả 0, không bịa số
        if (!string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            try
            {
                var e = AiPlateNormalizer.NormalizeEstimate(await _analyzer.EstimateIngredientAsync(name, grams, ct));
                return new AiResolvedIngredientModel(false, null, name, null, null, grams, "g", grams, e.Calories, e.Protein, e.Fat, e.Carbs, true, IconKeyResolver.Resolve(name));
            }
            catch (ExternalServiceException ex)
            {
                _logger.LogWarning(ex, "AI ingredient estimate failed for {Name}", name);
            }
        }
        return new AiResolvedIngredientModel(false, null, name, null, null, grams, "g", grams, 0, 0, 0, 0, false, IconKeyResolver.Resolve(name));
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
                f.Id, f.Name, f.Category, f.Source, f.IsVerified, Mine = f.CreatedByUserId == userId,
                f.CaloriesPer100g, f.ProteinPer100g, f.FatPer100g, f.CarbsPer100g,
                Servings = f.Servings.Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).ToList(),
            }).ToListAsync(ct))
            .Select(f => new Candidate(f.Id, f.Name, f.Category, f.Source, f.IsVerified, f.Mine, f.CaloriesPer100g, f.ProteinPer100g, f.FatPer100g, f.CarbsPer100g,
                f.Servings.Select(s => (s.Id, s.ServingName, s.WeightInGrams)).ToList()))
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

    private async Task<Candidate?> FromCanonicalAsync(Guid userId, List<string> keys, CancellationToken ct)
    {
        var canonicalId = await _uow.Repository<FoodAlias>().Query()
                .Where(a => a.CanonicalFoodId != null && keys.Contains(a.AliasNormalized)).Select(a => a.CanonicalFoodId).FirstOrDefaultAsync(ct)
            ?? await _uow.Repository<CanonicalFood>().Query()
                .Where(c => c.Status == CanonicalFoodStatus.Active && keys.Contains(c.NameNormalized)).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        return canonicalId is { } id ? await RepresentativeAsync(userId, id, ct) : null;
    }

    // Tìm rộng (chưa có canonical / dữ liệu cũ): mọi từ phải có trong tên hoặc alias, rồi xếp hạng bằng FoodMatchRanker
    private async Task<Candidate?> BroadAsync(Guid userId, string name, string queryNormalized, CancellationToken ct)
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

        return FoodMatchRanker.Rank(rows, queryNormalized, r => r.Name,
                r => aliases.GetValueOrDefault(r.Id) ?? [], r => AiIngredientMatcher.SourcePriority(r.Source, r.Verified, r.Mine))
            .First();
    }
}
