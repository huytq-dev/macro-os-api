namespace Application;

public sealed class SearchFoodsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SearchFoodsQuery, QueryResult<FoodItemModel>>
{
    public async Task<QueryResult<FoodItemModel>> Handle(SearchFoodsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var top = Math.Clamp(request.Top, 1, AppConstants.MaxPageSize);
        var skip = Math.Max(request.Skip, 0);

        // Cài đặt Food Log của user: chưa lưu = bật cả hai
        var prefs = await _uow.Repository<FoodLogSetting>().Query()
            .Where(s => s.UserId == userId)
            .Select(s => new { s.ShowBrandedResults, s.ShowOpenFoodFactsResults })
            .FirstOrDefaultAsync(ct);
        var showBranded = prefs?.ShowBrandedResults ?? true;
        var showOpenFoodFacts = prefs?.ShowOpenFoodFactsResults ?? true;

        var q = _uow.Repository<FoodItem>().Query()
            .Where(f => f.Source != FoodSource.QuickAdd);

        q = request.Scope switch
        {
            FoodScope.Mine   => q.Where(f => f.CreatedByUserId == userId),
            FoodScope.System => q.Where(f => f.Source != FoodSource.UserCreated),
            _                => q.Where(f => f.Source != FoodSource.UserCreated || f.CreatedByUserId == userId),
        };

        // Tắt branded → bỏ món có thương hiệu (món của chính user vẫn hiện); tắt OFF → bỏ món lấy từ Open Food Facts.
        // Chỉ lọc tìm kiếm; tra mã vạch (GET /foods/barcode) không bị lọc.
        if (!showBranded)
            q = q.Where(f => f.CreatedByUserId == userId || f.Brand == null || f.Brand == "");
        if (!showOpenFoodFacts)
            q = q.Where(f => f.Source != FoodSource.OpenFoodFacts);

        if (request.Category.HasValue)
            q = q.Where(f => f.Category == request.Category.Value);

        var search = request.Search?.Trim();
        var hasSearch = !string.IsNullOrEmpty(search);

        // Alias đã chuẩn hoá (lower, bỏ dấu): gõ "muoi"/"muối"/"salt" đều ra Salt và các biến thể cùng canonical. Khớp theo nguyên từ để từ ngắn không kéo mọi alias.
        // Id món/canonical có alias khớp được tải trước (một truy vấn nhỏ) rồi lọc bằng IN: subquery alias tương quan chạy lại cho từng món (OR chặn index) làm tìm 12k món USDA mất hàng giây
        var norm = FoodNameNormalizer.Normalize(search);
        var hits = hasSearch ? await AliasHits.LoadAsync(_uow, norm, ct) : AliasHits.None;
        var wordFoods = hits.WordFoodIds;
        var wordCanon = hits.WordCanonicalIds;
        var exactFoods = hits.ExactFoodIds;
        var exactCanon = hits.ExactCanonicalIds;

        if (hasSearch)
            q = q.Where(f => f.Name.Contains(search!) || (f.Brand != null && f.Brand.Contains(search!))
                || wordFoods.Contains(f.Id) || (f.CanonicalFoodId != null && wordCanon.Contains(f.CanonicalFoodId.Value)));

        // ponytail: LIKE %x% trên Name (quét ~12k dòng, vài chục ms); full-text/trigram khi dữ liệu lớn hơn nhiều
        // Một subquery gộp (món → lần log gần nhất của user) thay vì subquery tương quan theo từng dòng:
        // SQL quét FoodLogEntry của user một lần rồi join; món chưa log đi sau (ThenBy tên).
        var lastLogged = _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodItemId != null && e.FoodLog.UserId == userId)
            .GroupBy(e => e.FoodItemId)
            .Select(g => new { FoodItemId = g.Key, LastLoggedAt = g.Max(e => (DateTimeOffset?)e.CreatedAt) });

        var joined = q.GroupJoin(lastLogged, f => f.Id, l => l.FoodItemId,
            (f, l) => new { Food = f, Last = l.Select(x => x.LastLoggedAt).FirstOrDefault() });

        // Có từ khoá: xếp theo mức khớp trước (0 tên trùng hẳn / trước dấu phẩy / dòng đại diện khớp alias, 1 biến thể cùng canonical khớp alias, 2 tên bắt đầu bằng từ khoá, 3 còn lại)
        // để "Salt" và các loại muối đứng trước "Saltine Crackers" và món chỉ chứa chữ salt; cùng mức thì đại diện, rồi món log gần nhất, rồi tên ngắn.
        var ordered = !hasSearch
            ? joined.OrderByDescending(x => x.Last).ThenBy(x => x.Food.Name).ThenBy(x => x.Food.Id)
            : joined
                .OrderBy(x =>
                    x.Food.Name == search || x.Food.Name.StartsWith(search! + ",")
                    || (x.Food.IsRepresentative && (exactFoods.Contains(x.Food.Id) || (x.Food.CanonicalFoodId != null && exactCanon.Contains(x.Food.CanonicalFoodId.Value)))) ? 0
                    : exactFoods.Contains(x.Food.Id) || (x.Food.CanonicalFoodId != null && exactCanon.Contains(x.Food.CanonicalFoodId.Value)) ? 1
                    : x.Food.Name.StartsWith(search!) ? 2 : 3)
                .ThenByDescending(x => x.Food.IsRepresentative)
                .ThenByDescending(x => x.Last)
                // Món catalog/seed trước lớp USDA nhập hàng loạt (hàng nghìn món) khi cùng mức khớp; món user hay log vẫn lên trước nhờ Last
                .ThenBy(x => x.Food.ProviderCode == "usda" ? 1 : 0)
                .ThenBy(x => x.Food.Name.Length).ThenBy(x => x.Food.Name).ThenBy(x => x.Food.Id);

        var rows = await ordered
            .Skip(skip).Take(top)
            .Select(x => new
            {
                // Chỉ các cột cần hiển thị (không kéo cả entity ~35 cột).
                x.Food.Id, x.Food.Name, x.Food.Brand, x.Food.Category, x.Food.Source, x.Food.IsVerified,
                x.Food.CreatedByUserId, x.Food.IconKey,
                x.Food.CaloriesPer100g, x.Food.ProteinPer100g, x.Food.CarbsPer100g, x.Food.FatPer100g,
                x.Last,
                // Khẩu phần mặc định lấy một lần (Id + tên + gram cùng một dòng thay vì 3 subquery).
                Serving = x.Food.Servings.OrderByDescending(s => s.IsDefault)
                    .Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).FirstOrDefault(),
            })
            .ToListAsync(ct);

        // Trang đầu chưa đầy thì tổng = số dòng đã có, khỏi một truy vấn đếm
        var total = skip == 0 && rows.Count < top ? rows.Count : await q.CountAsync(ct);
        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, rows.Select(x => x.Id), ct);
        var items = rows.Select(x => new FoodItemModel(
            x.Id, x.Name, x.Brand, x.Category.ToString(), x.Source.ToString(), x.IsVerified,
            x.CreatedByUserId == userId,
            x.CaloriesPer100g, x.ProteinPer100g, x.CarbsPer100g, x.FatPer100g,
            x.Serving?.Id, x.Serving?.ServingName ?? "100 g", x.Serving?.WeightInGrams ?? 100m,
            x.Last, FoodEmoji.For(x.Category), FoodIconSupport.Effective(x.Id, x.IconKey, overrides))).ToList();

        return new QueryResult<FoodItemModel> { Items = items, TotalCount = total };
    }
}
