namespace Application;

/// <summary>
/// GET /foods/search: tìm món chia nhóm (From History / Common / Custom / Branded + Open Food Facts). Danh mục HỆ THỐNG (Common, Branded) so khớp trong bộ nhớ trên <see cref="FoodCatalogIndex"/> (đã gấp dấu/hoa thường, mỗi dòng mang
/// luôn cột hiển thị); món của user (Custom), lịch sử log và icon riêng đọc từ DB: 3 truy vấn nhỏ theo user mỗi lần gõ phím (trước: 7-8 truy vấn, một quét bảng cho mỗi nhóm, tải mọi món khớp về app). Logic so khớp/xếp hạng ở <see cref="FoodSearchContext"/>.
/// </summary>
public sealed class SearchFoodsGroupedQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOpenFoodFactsClient _off, IRedisCacheService _cache, ILogger<SearchFoodsGroupedQueryHandler> _logger)
    : IRequestHandler<SearchFoodsGroupedQuery, FoodSearchResponse>
{
    private static readonly TimeSpan OffCacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>Collation không phân biệt hoa/thường VÀ dấu cho các truy vấn SQL còn lại nếu có (đã kiểm: Latin1_General_100_CI_AI coi đ = d, ơ = o; Vietnamese_* thì không khớp "com" ↔ "Cơm"). Tìm theo từng phím không dùng SQL mà so khớp trên chỉ mục RAM đã gấp dấu.</summary>
    internal const string Collation = "Latin1_General_100_CI_AI";

    public async Task<FoodSearchResponse> Handle(SearchFoodsGroupedQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var search = request.Search?.Trim() ?? "";
        var hasSearch = search.Length > 0;
        var section = request.Section;
        var top = request.Top > 0 ? request.Top : section is null ? SearchFoodsGroupedQuery.OverviewTop : SearchFoodsGroupedQuery.SectionTop;
        var skip = section is null ? 0 : (Math.Max(request.Page, 1) - 1) * top;

        // Cài đặt Food Log của user (chưa lưu = bật cả hai): cache ngắn theo user, UpdateFoodLogSettings ghi đè cache ngay khi đổi
        var (showBranded, showOff) = await FoodSearchPrefs.GetAsync(_uow, userId, ct);

        bool Wants(FoodSearchSection s) => section is null || section == s;

        var ctx = await FoodSearchContext.LoadAsync(_uow, userId, search, ct);

        // Open Food Facts là cuộc gọi mạng độc lập với DbContext: chạy song song với phần còn lại (chậm tới 3 giây nếu OFF chậm)
        var offTask = hasSearch && search.Length >= 2 && showOff && Wants(FoodSearchSection.OpenFoodFacts)
            ? SearchOffAsync(search, ctx.Norm, section is null ? 1 : Math.Max(request.Page, 1), top, ct)
            : Task.FromResult(OffSearchGroup.Unavailable);

        var empty = DateTimeOffset.MinValue;
        IEnumerable<FoodCatalogIndex.Entry> Systematic() => hasSearch ? ctx.Index.Entries.Where(ctx.Matches) : [];
        IEnumerable<FoodCatalogIndex.Entry> OwnMatching() => ctx.Own.Where(ctx.Matches);

        // Mỗi nhóm: lọc + xếp hạng + phân trang trên tập đã khớp (tổng = số món khớp của nhóm). OrderBy rồi Take(skip + top): .NET chỉ sắp một phần, không sắp cả nghìn dòng
        (List<FoodCatalogIndex.Entry> Page, int Total) Group(bool wanted, IEnumerable<FoodCatalogIndex.Entry> candidates, Func<IEnumerable<FoodCatalogIndex.Entry>, IOrderedEnumerable<FoodCatalogIndex.Entry>> order)
        {
            if (!wanted) return ([], 0);
            var rows = candidates.ToList();
            return (order(rows).Take(skip + top).Skip(skip).ToList(), rows.Count);
        }

        // From History: món user đã log (hệ thống hoặc của user), gần nhất trước; không từ khoá = các món log gần đây
        var historySource = hasSearch
            ? Systematic().Concat(OwnMatching()).Where(e => ctx.LastByFood.ContainsKey(e.Id))
            : ctx.LastByFood.Keys.Select(ctx.FindLogged).OfType<FoodCatalogIndex.Entry>();
        var history = Group(Wants(FoodSearchSection.History), historySource,
            r => r.OrderByDescending(e => ctx.LastByFood[e.Id]).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id));
        // Common: catalog + USDA (không thương hiệu): mức khớp > món hay log gần đây > đại diện/verified > catalog trước USDA > tên ngắn. Món đã log VẪN nằm ở đây (không loại khỏi History)
        var common = Group(hasSearch && Wants(FoodSearchSection.Common), Systematic().Where(e => FoodSearchContext.IsSystemSource(e.Source) && !e.HasBrand),
            r => r.OrderBy(ctx.Tier).ThenByDescending(e => ctx.LastOf(e) ?? empty).ThenByDescending(e => e.IsRepresentative).ThenByDescending(e => e.IsVerified).ThenBy(e => e.ProviderCode == "usda" ? 1 : 0)
                .ThenBy(e => e.Name.Length).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id));
        // Custom: món user tự tạo (không từ khoá = các món tự tạo gần dùng)
        var custom = Group(Wants(FoodSearchSection.Custom), OwnMatching(),
            r => r.OrderBy(ctx.Tier).ThenByDescending(e => ctx.LastOf(e) ?? empty).ThenBy(e => e.Name.Length).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id));
        // Branded: món có thương hiệu đã có trong DB (sản phẩm OFF từng quét mã vạch…); tắt "Branded" → rỗng, tắt "Open Food Facts" → bỏ món OFF
        var branded = Group(hasSearch && showBranded && Wants(FoodSearchSection.Branded), Systematic().Where(e => e.HasBrand && (showOff || e.Source != FoodSource.OpenFoodFacts)),
            r => r.OrderBy(ctx.Tier).ThenByDescending(e => ctx.LastOf(e) ?? empty).ThenBy(e => e.Name.Length).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id));

        // Icon riêng của user cho các dòng của trang: MỘT truy vấn nhỏ (icon của user thắng icon của món)
        var pageIds = history.Page.Concat(common.Page).Concat(custom.Page).Concat(branded.Page).Select(e => e.Id).Distinct().ToList();
        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, pageIds, ct);

        FoodSearchGroup Map((List<FoodCatalogIndex.Entry> Page, int Total) g) => new(g.Page.Select(e => ctx.ToModel(e, userId, overrides)).ToList(), g.Total);
        return new FoodSearchResponse(search, Map(history), Map(common), Map(custom), Map(branded), await offTask);
    }

    // Có cache (tìm cùng từ khoá nhiều lần không hỏi lại OFF); lỗi/timeout → nhóm không khả dụng, không làm hỏng cả trang
    private async Task<OffSearchGroup> SearchOffAsync(string search, string norm, int page, int pageSize, CancellationToken ct)
    {
        var key = $"foodsearch:off:{norm}:{page}:{pageSize}";
        try
        {
            if (await _cache.GetRecordAsync<OffSearchGroup>(key) is { Available: true } cached) return cached;
            var result = await _off.SearchByNameAsync(search, page, pageSize, ct);
            var group = new OffSearchGroup(result.Items.Select(p => new OffSearchItem(
                p.Code, p.Name, p.Brand, p.ImageUrl, p.Nutrition.CaloriesPer100g, p.Nutrition.ProteinPer100g, p.Nutrition.CarbsPer100g, p.Nutrition.FatPer100g, p.ServingName, p.ServingGrams)).ToList(),
                result.TotalCount, true);
            await _cache.SetRecordAsync(key, group, OffCacheTtl);
            return group;
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogInformation("Open Food Facts search unavailable: {Message}", ex.Message);
            return OffSearchGroup.Unavailable;
        }
    }
}
