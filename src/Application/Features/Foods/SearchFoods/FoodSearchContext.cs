namespace Application;

/// <summary>
/// Logic tìm món dùng chung cho GET /foods (danh sách phẳng, FE đang dùng) và GET /foods/search (chia nhóm): so khớp trong RAM trên <see cref="FoodCatalogIndex"/> (danh mục hệ thống) cộng món tự tạo của user,
/// mức khớp, ưu tiên món user hay log gần đây, và các truy vấn DB nhỏ theo user (lần log gần nhất, món tự tạo; icon riêng nạp theo id của trang).
/// </summary>
public sealed class FoodSearchContext
{
    private static readonly FoodSource[] SystemSources = [FoodSource.System, FoodSource.USDA, FoodSource.VnFct];

    private readonly HashSet<Guid> _wordFoods = [], _wordCanon = [], _exactFoods = [], _exactCanon = [];
    private readonly string[][] _groups;
    private readonly string _commaPrefix, _wordPrefix;

    public FoodSearchContext(FoodCatalogIndex index, IReadOnlyList<FoodCatalogIndex.Entry> own, IReadOnlyDictionary<Guid, DateTimeOffset> lastByFood, string? search)
    {
        Index = index; Own = own; LastByFood = lastByFood;
        Search = search?.Trim() ?? "";
        HasSearch = Search.Length > 0;
        Folded = FoodCatalogIndex.Fold(Search);
        _groups = FoodSearchSynonyms.Groups(Folded);
        _commaPrefix = Folded + ",";
        _wordPrefix = " " + Folded;

        // Alias khớp (nguyên từ, đã chuẩn hoá bỏ dấu): gõ "muoi"/"muối"/"salt" đều ra Salt và các biến thể cùng canonical
        var norm = FoodNameNormalizer.Normalize(Search);
        Norm = norm;
        if (HasSearch && norm.Length >= 2)
            foreach (var a in index.Aliases)
            {
                var exact = a.Normalized == norm;
                if (!exact && !(a.Normalized.StartsWith(norm + " ", StringComparison.Ordinal) || a.Normalized.EndsWith(" " + norm, StringComparison.Ordinal) || a.Normalized.Contains(" " + norm + " ", StringComparison.Ordinal))) continue;
                _wordFoods.Add(a.FoodItemId); if (a.CanonicalFoodId is { } wc) _wordCanon.Add(wc);
                if (exact) { _exactFoods.Add(a.FoodItemId); if (a.CanonicalFoodId is { } ec) _exactCanon.Add(ec); }
            }
    }

    public FoodCatalogIndex Index { get; }
    public IReadOnlyList<FoodCatalogIndex.Entry> Own { get; }
    public IReadOnlyDictionary<Guid, DateTimeOffset> LastByFood { get; }
    public string Search { get; }
    public string Norm { get; }
    public string Folded { get; }
    public bool HasSearch { get; }

    public static bool IsSystemSource(FoodSource s) => SystemSources.Contains(s);

    private bool InExact(FoodCatalogIndex.Entry e) => _exactFoods.Contains(e.Id) || (e.CanonicalFoodId is { } c && _exactCanon.Contains(c));
    private bool AliasHit(FoodCatalogIndex.Entry e) => _wordFoods.Contains(e.Id) || (e.CanonicalFoodId is { } c && _wordCanon.Contains(c));

    // Từ MỘT ký tự chỉ khớp ĐẦU TỪ ("c" không kéo cả nghìn món chứa chữ c giữa từ); từ dài hơn khớp ở bất kỳ đâu
    private static bool Token(string hay, string t) => t.Length == 1 ? hay.StartsWith(t, StringComparison.Ordinal) || hay.Contains(" " + t, StringComparison.Ordinal) : hay.Contains(t, StringComparison.Ordinal);

    /// <summary>Mọi từ của từ khoá (hoặc một đồng nghĩa của nó, xem <see cref="FoodSearchSynonyms"/>) nằm trong tên hoặc thương hiệu (thứ tự bất kỳ, không phân biệt hoa/thường/dấu), hoặc khớp alias. Không từ khoá: khớp hết.</summary>
    public bool Matches(FoodCatalogIndex.Entry e)
        => !HasSearch || _groups.All(g => g.Any(t => Token(e.NameFolded, t) || (e.BrandFolded.Length > 0 && Token(e.BrandFolded, t)))) || AliasHit(e);

    /// <summary>0 tên trùng hẳn / trước dấu phẩy / dòng đại diện khớp alias, 1 biến thể cùng canonical khớp alias, 2 tên bắt đầu bằng từ khoá, 3 một TỪ của tên bắt đầu bằng từ khoá ("Fish, tuna" cho "tuna"), 4 còn lại. Không từ khoá: 0.</summary>
    public int Tier(FoodCatalogIndex.Entry e) => !HasSearch ? 0
        : e.NameFolded == Folded || e.NameFolded.StartsWith(_commaPrefix, StringComparison.Ordinal) || (e.IsRepresentative && InExact(e)) ? 0
        : InExact(e) ? 1
        : e.NameFolded.StartsWith(Folded, StringComparison.Ordinal) ? 2
        : e.NameFolded.Contains(_wordPrefix, StringComparison.Ordinal) ? 3 : 4;

    public DateTimeOffset? LastOf(FoodCatalogIndex.Entry e) => LastByFood.TryGetValue(e.Id, out var l) ? l : null;

    /// <summary>Mọi món có thể xuất hiện: danh mục hệ thống rồi món tự tạo của user.</summary>
    public IEnumerable<FoodCatalogIndex.Entry> All => Index.Entries.Concat(Own);

    public FoodCatalogIndex.Entry? FindLogged(Guid id) => Index.Find(id) ?? Own.FirstOrDefault(o => o.Id == id);

    public FoodItemModel ToModel(FoodCatalogIndex.Entry e, Guid userId, IReadOnlyDictionary<Guid, string> iconOverrides)
        => new(e.Id, e.Name, e.Brand, e.Category.ToString(), e.Source.ToString(), e.IsVerified, e.CreatedByUserId == userId,
            e.Calories, e.Protein, e.Carbs, e.Fat, e.ServingId, e.ServingName, e.ServingGrams,
            LastOf(e), FoodEmoji.For(e.Category), FoodIconSupport.Effective(e.Id, e.IconKey, iconOverrides));

    /// <summary>Nạp các phần theo user (hai truy vấn nhỏ: lần log gần nhất, món tự tạo) cùng chỉ mục danh mục dùng chung rồi dựng ngữ cảnh tìm.</summary>
    public static async Task<FoodSearchContext> LoadAsync(IUnitOfWork uow, Guid userId, string? search, CancellationToken ct)
    {
        var index = await FoodCatalogIndexCache.GetAsync(uow, ct);

        // Lần log gần nhất của user cho từng món (một truy vấn gộp theo món, số dòng = số món user từng log)
        var lastByFood = (await uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodItemId != null && e.FoodLog.UserId == userId)
            .GroupBy(e => e.FoodItemId!.Value)
            .Select(g => new { FoodItemId = g.Key, Last = g.Max(e => e.CreatedAt) })
            .ToListAsync(ct)).ToDictionary(x => x.FoodItemId, x => x.Last);

        // Món user tự tạo (nhỏ, theo user): so khớp bằng CÙNG đường trong RAM với danh mục hệ thống
        var own = (await uow.Repository<FoodItem>().Query().Where(f => f.Source == FoodSource.UserCreated && f.CreatedByUserId == userId)
            .Select(f => new
            {
                f.Id, f.Name, f.Brand, f.Source, f.ProviderCode, f.IsRepresentative, f.IsVerified, f.CanonicalFoodId, f.Category, f.IconKey, f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
                Serving = f.Servings.OrderByDescending(s => s.IsDefault).Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).FirstOrDefault(),
            }).ToListAsync(ct))
            .Select(r => FoodCatalogIndex.ToEntry(r.Id, r.Name, r.Brand, r.Source, r.ProviderCode, r.IsRepresentative, r.IsVerified, r.CanonicalFoodId, r.Category, r.IconKey,
                r.CaloriesPer100g, r.ProteinPer100g, r.CarbsPer100g, r.FatPer100g, r.Serving?.Id, r.Serving?.ServingName, r.Serving?.WeightInGrams, userId)).ToList();

        return new FoodSearchContext(index, own, lastByFood, search);
    }
}
