namespace Application;

/// <summary>
/// Pha 2 của AI Plate: tìm một nguyên liệu (tên tiếng Anh hoặc Việt + gram) trong DB thực phẩm và tính macro từ món khớp.
/// Thứ tự: bộ nhớ đệm của user (món đã xác nhận, khoá có thêm state) → thực phẩm chuẩn (qua alias/tên chuẩn hoá; chọn đúng biến thể theo tên hẳn hoặc state, không có thì dòng đại diện)
/// → tìm rộng có xếp hạng. Không khớp → AI ước lượng (Estimated=true) hoặc 0 (không bịa số). Không ghi nhật ký (chỉ cập nhật HitCount của bộ nhớ đệm khi trúng).
/// <paramref name="Quantity"/> + <paramref name="Unit"/> (tuỳ chọn, từ AI): đếm được ("3 large") thì chọn khẩu phần có đúng tên đơn vị; không có/không khớp thì chọn khẩu phần theo gram như cũ.
/// <paramref name="State"/> (tuỳ chọn): raw | boiled | fried | scrambled | grilled | steamed | baked | roasted | stir_fried | none.
/// </summary>
public sealed record ResolveAiIngredientQuery(string Name, decimal Grams, decimal? Quantity = null, string? Unit = null, string? State = null) : IRequest<AiResolvedIngredientModel>;

public sealed class ResolveAiIngredientQueryValidator : AbstractValidator<ResolveAiIngredientQuery>
{
    public ResolveAiIngredientQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Grams).GreaterThan(0).LessThanOrEqualTo(5000);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Unit).Must(u => AiIngredientResolution.NormalizeUnit(u) is not null)
            .WithMessage(ValidationMessages.InvalidEnumValue).When(x => !string.IsNullOrWhiteSpace(x.Unit));
        RuleFor(x => x.State).Must(FoodPreparationState.IsValid).WithMessage(ValidationMessages.InvalidEnumValue);
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

/// <summary>Quy tắc chọn biến thể + khẩu phần đếm được cho pha resolve (hàm thuần, kiểm bằng AlgorithmChecks).</summary>
public static class AiIngredientResolution
{
    public const string SourceCache = "cache", SourceCanonical = "canonical", SourceVariant = "variant", SourceSearch = "search", SourceAi = "ai", SourceNone = "none";

    /// <summary>Sai lệch tối đa giữa gram AI ước và Quantity × gram khẩu phần; vượt thì tin khẩu phần trong DB.</summary>
    public const decimal ServingTolerance = 0.4m;

    private static readonly HashSet<string> SizeUnits = new(StringComparer.Ordinal) { "small", "medium", "large", "extra large" };

    // Khẩu phần "cỡ" (trứng/chuối/táo…): chỉ chọn khi AI nêu đơn vị cỡ; khi chọn theo gram thì bỏ qua để 150 g trứng ra "3 piece" chứ không ra "2.4 jumbo"
    private static readonly HashSet<string> SizeLabels = new(StringComparer.OrdinalIgnoreCase) { "extra small", "small", "medium", "large", "extra large", "jumbo" };

    /// <summary>Các khẩu phần dùng để chọn theo gram: bỏ khẩu phần cỡ (trừ khi món chỉ có khẩu phần cỡ).</summary>
    public static List<(Guid Id, string Name, decimal Grams)> ForGramChoice(IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings)
    {
        var plain = servings.Where(s => !SizeLabels.Contains(ServingLabel(s.Name))).ToList();
        return plain.Count > 0 ? plain : servings.ToList();
    }

    /// <summary>Đơn vị đếm AI được phép trả (khớp enum của tool report_meal).</summary>
    public static readonly IReadOnlySet<string> Units = new HashSet<string>(StringComparer.Ordinal)
    {
        "small", "medium", "large", "extra large", "slice", "piece", "cup", "tbsp", "tsp", "bowl", "plate",
    };

    /// <summary>Chữ thường, "_"/"-" → khoảng trắng; không thuộc <see cref="Units"/> → null.</summary>
    public static string? NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;
        var u = unit.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        return Units.Contains(u) ? u : null;
    }

    /// <summary>Khoá bộ nhớ đệm: tên chuẩn hoá, có state thì thêm hậu tố "|state" (state none = khoá cũ).</summary>
    public static string CacheKey(string normalized, string state)
        => state == FoodPreparationState.None ? normalized : $"{normalized}|{state}";

    /// <summary>Bỏ tiền tố "1 " của tên khẩu phần để FE ghép "3 large" ("1 large" → "large"); các tên khác giữ nguyên.</summary>
    public static string ServingLabel(string name)
        => name.StartsWith("1 ", StringComparison.Ordinal) ? name[2..].Trim() : name;

    /// <summary>
    /// Khẩu phần có tên đúng đơn vị (sau khi bỏ "1 "); không có thì khẩu phần chứa nguyên từ đơn vị (ngắn nhất). Đơn vị cỡ (small/medium/large/extra large) chỉ khớp đúng tên
    /// để "large" không rơi vào "extra large". Không khớp → null (rơi về chọn theo gram).
    /// </summary>
    public static (Guid Id, string Name, decimal Grams)? MatchServing(IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings, string unit)
    {
        var usable = servings.Where(s => s.Grams > 0).Select(s => ((Guid Id, string Name, decimal Grams)?)s).ToList();
        var exact = usable.FirstOrDefault(s => ServingLabel(s!.Value.Name).Equals(unit, StringComparison.OrdinalIgnoreCase));
        if (exact is not null || SizeUnits.Contains(unit)) return exact;
        return usable.Where(s => $" {ServingLabel(s!.Value.Name).ToLowerInvariant()} ".Contains($" {unit} "))
            .OrderBy(s => s!.Value.Name.Length).FirstOrDefault();
    }

    /// <summary>Khẩu phần trả cho FE (chip đổi cỡ): bỏ gram ≤ 0, nhãn bỏ tiền tố "1 ", mặc định đứng đầu rồi theo gram tăng dần.</summary>
    public static List<AiServingModel> ServingOptions(IEnumerable<(Guid Id, string Name, decimal Weight, bool IsDefault)> servings)
        => servings.Where(s => s.Weight > 0).OrderByDescending(s => s.IsDefault).ThenBy(s => s.Weight).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => new AiServingModel(s.Id, ServingLabel(s.Name), s.Weight, s.IsDefault)).ToList();

    public sealed record UnitServing(Guid ServingId, string Label, decimal Quantity, decimal WeightGrams, bool Corrected);

    /// <summary>
    /// Chọn khẩu phần theo đơn vị đếm. Số lượng = <paramref name="quantity"/> (thiếu thì suy từ gram, chỉ nhận 0.25–4 khẩu phần); gram = số lượng × gram khẩu phần.
    /// <see cref="UnitServing.Corrected"/> = lệch quá <see cref="ServingTolerance"/> so với gram AI ước (khi đó tin khẩu phần DB, nơi gọi ghi log).
    /// </summary>
    public static UnitServing? ChooseByUnit(IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings, string unit, decimal? quantity, decimal grams)
    {
        if (MatchServing(servings, unit) is not { } s) return null;
        if (quantity is null)
        {
            var ratio = grams / s.Grams;
            if (ratio is < 0.25m or > 4m) return null;
        }
        var qty = quantity ?? NutritionRounding.Round(grams / s.Grams, 2);
        if (qty <= 0) return null;
        var weight = NutritionRounding.Round(qty * s.Grams, 1);
        var corrected = quantity is not null && grams > 0 && Math.Abs(weight - grams) / grams > ServingTolerance;
        return new UnitServing(s.Id, ServingLabel(s.Name), qty, weight, corrected);
    }

    /// <summary>
    /// Chọn món trong các món cùng thực phẩm chuẩn: (1) tên món trùng hẳn tên hỏi (vd "Olive Oil") (2) món có cùng state (vd fried) (3) dòng đại diện.
    /// Trả null khi không có món nào dùng được. Source = canonical (dòng đại diện) hoặc variant (biến thể khác).
    /// </summary>
    public static (T Item, string Source)? PickFood<T>(IReadOnlyList<T> items, Func<T, string> name, Func<T, bool> isRep, IReadOnlyCollection<string> keys, string state)
        where T : class
    {
        if (items.Count == 0) return null;
        var picked = items.Where(i => keys.Contains(FoodNameNormalizer.Normalize(name(i)))).OrderByDescending(isRep).FirstOrDefault();
        if (picked is null && state != FoodPreparationState.None)
            picked = items.Where(i => FoodPreparationState.Detect(name(i)) == state).OrderByDescending(isRep).ThenBy(i => name(i).Length).FirstOrDefault();
        picked ??= items.FirstOrDefault(isRep);
        return picked is null ? null : (picked, isRep(picked) ? SourceCanonical : SourceVariant);
    }
}

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
