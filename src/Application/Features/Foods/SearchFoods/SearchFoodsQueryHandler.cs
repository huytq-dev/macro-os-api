namespace Application;

/// <summary>
/// GET /foods (danh sách phẳng; endpoint FE đang dùng cho tìm món, Foods page, chọn nguyên liệu): cùng bộ so khớp trong RAM với GET /foods/search (<see cref="FoodSearchContext"/>): không phân biệt hoa/thường/dấu ("pho" ra "Phở"),
/// mức khớp tên-đầu / đầu-từ / chứa, món user hay log gần đây lên trước; 2 truy vấn nhỏ theo user + icon riêng của trang (trước: một truy vấn quét bảng + đếm + alias + icon + cài đặt, tải ~35 cột).
/// </summary>
public sealed class SearchFoodsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SearchFoodsQuery, QueryResult<FoodItemModel>>
{
    public async Task<QueryResult<FoodItemModel>> Handle(SearchFoodsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var top = Math.Clamp(request.Top, 1, AppConstants.MaxPageSize);
        var skip = Math.Max(request.Skip, 0);

        // Cài đặt Food Log của user: chưa lưu = bật cả hai (cache ngắn, xem FoodSearchPrefs)
        var (showBranded, showOpenFoodFacts) = await FoodSearchPrefs.GetAsync(_uow, userId, ct);

        var ctx = await FoodSearchContext.LoadAsync(_uow, userId, request.Search, ct);

        // Phạm vi: Mine = món tự tạo của tôi; System = món hệ thống; All = cả hai (không gồm custom của người khác, không gồm QuickAdd: chỉ mục không chứa chúng)
        IEnumerable<FoodCatalogIndex.Entry> pool = request.Scope switch
        {
            FoodScope.Mine => ctx.Own,
            FoodScope.System => ctx.Index.Entries,
            _ => ctx.All,
        };

        // Tắt branded → bỏ món có thương hiệu (món của chính user vẫn hiện); tắt OFF → bỏ món lấy từ Open Food Facts. Chỉ lọc tìm kiếm; tra mã vạch (GET /foods/barcode) không bị lọc.
        var rows = pool.Where(e => (showBranded || e.CreatedByUserId == userId || !e.HasBrand)
                                   && (showOpenFoodFacts || e.Source != FoodSource.OpenFoodFacts)
                                   && (request.Category is not { } cat || e.Category == cat)
                                   && ctx.Matches(e)).ToList();

        // Có từ khoá: mức khớp, món user hay log gần đây, đại diện, catalog trước USDA, tên ngắn. Không từ khoá: món log gần nhất trước rồi theo tên (OrderBy + Take(skip + top): sắp một phần, không sắp cả danh mục)
        var empty = DateTimeOffset.MinValue;
        var ordered = !ctx.HasSearch
            ? rows.OrderByDescending(e => ctx.LastOf(e) ?? empty).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id)
            : rows.OrderBy(ctx.Tier).ThenByDescending(e => ctx.LastOf(e) ?? empty).ThenByDescending(e => e.IsRepresentative).ThenBy(e => e.ProviderCode == "usda" ? 1 : 0)
                .ThenBy(e => e.Name.Length).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id);
        var page = ordered.Take(skip + top).Skip(skip).ToList();

        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, page.Select(e => e.Id), ct);
        return new QueryResult<FoodItemModel> { Items = page.Select(e => ctx.ToModel(e, userId, overrides)).ToList(), TotalCount = rows.Count };
    }
}
