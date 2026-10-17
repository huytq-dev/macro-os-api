namespace Application;

public enum FoodScope { All, Mine, System }

/// <summary>UC-C01: tìm theo tên/brand; All = hệ thống + của tôi (không gồm custom của người khác, không gồm QuickAdd).</summary>
public sealed record SearchFoodsQuery(
    string? Search,
    FoodCategory? Category,
    FoodScope Scope = FoodScope.All,
    int Top = AppConstants.DefaultPageTop,
    int Skip = AppConstants.DefaultPageSkip
) : IRequest<QueryResult<FoodItemModel>>;

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
        if (!string.IsNullOrEmpty(search))
            q = q.Where(f => f.Name.Contains(search) || (f.Brand != null && f.Brand.Contains(search)));

        // ponytail: LIKE %x% trên index Name; full-text/ranking khi dữ liệu USDA vào
        var total = await q.CountAsync(ct);
        // Một subquery gộp (món → lần log gần nhất của user) thay vì subquery tương quan theo từng dòng:
        // SQL quét FoodLogEntry của user một lần rồi join; món chưa log đi sau (ThenBy tên).
        var lastLogged = _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodItemId != null && e.FoodLog.UserId == userId)
            .GroupBy(e => e.FoodItemId)
            .Select(g => new { FoodItemId = g.Key, LastLoggedAt = g.Max(e => (DateTimeOffset?)e.CreatedAt) });

        var rows = await q
            .GroupJoin(lastLogged, f => f.Id, l => l.FoodItemId,
                (f, l) => new { Food = f, Last = l.Select(x => x.LastLoggedAt).FirstOrDefault() })
            .OrderByDescending(x => x.Last)
            .ThenBy(x => x.Food.Name).ThenBy(x => x.Food.Id)
            .Skip(skip).Take(top)
            .Select(x => new
            {
                // Chỉ các cột cần hiển thị (không kéo cả entity ~35 cột).
                x.Food.Id, x.Food.Name, x.Food.Brand, x.Food.Category, x.Food.Source, x.Food.IsVerified,
                x.Food.CreatedByUserId,
                x.Food.CaloriesPer100g, x.Food.ProteinPer100g, x.Food.CarbsPer100g, x.Food.FatPer100g,
                x.Last,
                // Khẩu phần mặc định lấy một lần (Id + tên + gram cùng một dòng thay vì 3 subquery).
                Serving = x.Food.Servings.OrderByDescending(s => s.IsDefault)
                    .Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows.Select(x => new FoodItemModel(
            x.Id, x.Name, x.Brand, x.Category.ToString(), x.Source.ToString(), x.IsVerified,
            x.CreatedByUserId == userId,
            x.CaloriesPer100g, x.ProteinPer100g, x.CarbsPer100g, x.FatPer100g,
            x.Serving?.Id, x.Serving?.ServingName ?? "100 g", x.Serving?.WeightInGrams ?? 100m,
            x.Last, FoodEmoji.For(x.Category))).ToList();

        return new QueryResult<FoodItemModel> { Items = items, TotalCount = total };
    }
}
