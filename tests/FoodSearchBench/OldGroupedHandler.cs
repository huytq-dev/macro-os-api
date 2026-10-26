using Application;
using Contract;
using Domain;
using MediatR;
using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Options;
namespace Application;

public sealed class OldSearchFoodsGroupedQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOpenFoodFactsClient _off, IRedisCacheService _cache, ILogger<OldSearchFoodsGroupedQueryHandler> _logger)
    : IRequestHandler<SearchFoodsGroupedQuery, FoodSearchResponse>
{
    private static readonly TimeSpan OffCacheTtl = TimeSpan.FromMinutes(10);

    // Cột đủ để phân nhóm + xếp hạng một món khớp (truy vấn hẹp, một lần quét bảng); chi tiết chỉ lấy cho các dòng của trang
    private sealed record Slim(Guid Id, string Name, string? Brand, FoodSource Source, string? ProviderCode, bool IsRepresentative, bool IsVerified, Guid? CanonicalFoodId, Guid? CreatedByUserId);

    public async Task<FoodSearchResponse> Handle(SearchFoodsGroupedQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var search = request.Search?.Trim() ?? "";
        var hasSearch = search.Length > 0;
        var section = request.Section;
        var top = request.Top > 0 ? request.Top : section is null ? SearchFoodsGroupedQuery.OverviewTop : SearchFoodsGroupedQuery.SectionTop;
        var skip = section is null ? 0 : (Math.Max(request.Page, 1) - 1) * top;

        // Cài đặt Food Log của user: chưa lưu = bật cả hai
        var prefs = await _uow.Repository<FoodLogSetting>().Query().Where(s => s.UserId == userId)
            .Select(s => new { s.ShowBrandedResults, s.ShowOpenFoodFactsResults }).FirstOrDefaultAsync(ct);
        var showBranded = prefs?.ShowBrandedResults ?? true;
        var showOff = prefs?.ShowOpenFoodFactsResults ?? true;

        bool Wants(FoodSearchSection s) => section is null || section == s;
        var norm = FoodNameNormalizer.Normalize(search);

        // Open Food Facts là cuộc gọi mạng độc lập với DbContext: chạy song song với các truy vấn DB (chậm tới 3 giây nếu OFF chậm)
        var offTask = hasSearch && search.Length >= 2 && showOff && Wants(FoodSearchSection.OpenFoodFacts)
            ? SearchOffAsync(search, norm, section is null ? 1 : Math.Max(request.Page, 1), top, ct)
            : Task.FromResult(OffSearchGroup.Unavailable);

        // Lần log gần nhất của user cho từng món (một truy vấn, theo log của chính user nên nhỏ)
        var lastByFood = (await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodItemId != null && e.FoodLog.UserId == userId)
            .GroupBy(e => e.FoodItemId!.Value)
            .Select(g => new { FoodItemId = g.Key, Last = g.Max(e => e.CreatedAt) })
            .ToListAsync(ct)).ToDictionary(x => x.FoodItemId, x => (DateTimeOffset?)x.Last);

        // Alias khớp tải trước (một truy vấn nhỏ) rồi lọc bằng IN: subquery alias tương quan chạy lại cho từng món USDA làm tìm mất hàng giây
        var hits = hasSearch ? await AliasHits.LoadAsync(_uow, norm, ct) : AliasHits.None;
        var wordFoods = hits.WordFoodIds;
        var wordCanon = hits.WordCanonicalIds;
        var exactFoods = hits.ExactFoodIds.ToHashSet();
        var exactCanon = hits.ExactCanonicalIds.ToHashSet();
        var loggedIds = lastByFood.Keys.ToList();

        // MỘT lần quét FoodItems cho mọi nhóm. Có từ khoá: mọi món khớp (tên/thương hiệu/alias). Không từ khoá: chỉ món đã log và món tự tạo của user.
        // Khớp theo TỪNG TỪ (mọi từ của từ khoá phải nằm trong tên hoặc thương hiệu, thứ tự bất kỳ): "tomato soup" tìm ra "Soup, tomato"; một từ thì như trước. Cộng thêm món khớp alias.
        var tokens = search.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var foods = _uow.Repository<FoodItem>().Query().Where(f => f.Source != FoodSource.QuickAdd);
        IQueryable<FoodItem> byText;
        if (hasSearch)
        {
            byText = foods;
            foreach (var token in tokens)
            {
                var t = token;
                byText = byText.Where(f => f.Name.Contains(t) || (f.Brand != null && f.Brand.Contains(t)));
            }
        }
        else byText = foods.Where(f => loggedIds.Contains(f.Id) || (f.Source == FoodSource.UserCreated && f.CreatedByUserId == userId));
        var cols = (IQueryable<FoodItem> q) => q.Select(f => new { f.Id, f.Name, f.Brand, f.Source, f.ProviderCode, f.IsRepresentative, f.IsVerified, f.CanonicalFoodId, f.CreatedByUserId });
        var rows = await cols(byText).ToListAsync(ct);
        if (hasSearch && (wordFoods.Count > 0 || wordCanon.Count > 0))
        {
            var seen = rows.Select(r => r.Id).ToHashSet();
            rows.AddRange((await cols(foods.Where(f => wordFoods.Contains(f.Id) || (f.CanonicalFoodId != null && wordCanon.Contains(f.CanonicalFoodId.Value)))).ToListAsync(ct)).Where(r => seen.Add(r.Id)));
        }
        var matched = rows.Select(f => new Slim(f.Id, f.Name, f.Brand, f.Source, f.ProviderCode, f.IsRepresentative, f.IsVerified, f.CanonicalFoodId, f.CreatedByUserId)).ToList();

        // Mức khớp: 0 tên trùng hẳn / trước dấu phẩy / dòng đại diện khớp alias, 1 biến thể cùng canonical khớp alias, 2 tên bắt đầu bằng từ khoá, 3 còn lại
        bool InExact(Slim s) => exactFoods.Contains(s.Id) || (s.CanonicalFoodId is { } c && exactCanon.Contains(c));
        int Tier(Slim s) => !hasSearch ? 0
            : s.Name.Equals(search, StringComparison.OrdinalIgnoreCase) || s.Name.StartsWith(search + ",", StringComparison.OrdinalIgnoreCase) || (s.IsRepresentative && InExact(s)) ? 0
            : InExact(s) ? 1
            : s.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 2 : 3;
        DateTimeOffset? LastOf(Slim s) => lastByFood.GetValueOrDefault(s.Id);
        var systemSources = new[] { FoodSource.System, FoodSource.USDA, FoodSource.VnFct };

        // Mỗi nhóm: lọc + xếp hạng + phân trang trên danh sách hẹp (tổng = số món khớp của nhóm)
        (List<Slim> Page, int Total) Group(bool wanted, Func<Slim, bool> filter, Func<IEnumerable<Slim>, IOrderedEnumerable<Slim>> order)
        {
            if (!wanted) return ([], 0);
            var rows = matched.Where(filter).ToList();
            return (order(rows).Skip(skip).Take(top).ToList(), rows.Count);
        }

        // From History: món user đã log, gần nhất trước (không từ khoá = các món log gần đây)
        var history = Group(Wants(FoodSearchSection.History), s => lastByFood.ContainsKey(s.Id),
            r => r.OrderByDescending(LastOf).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id));
        // Common: catalog + USDA (không thương hiệu): khớp đầu tên > đại diện/verified > catalog trước USDA > tên ngắn. Món đã log VẪN nằm ở đây (không loại trừ khỏi History).
        var common = Group(hasSearch && Wants(FoodSearchSection.Common), s => systemSources.Contains(s.Source) && string.IsNullOrEmpty(s.Brand),
            r => r.OrderBy(Tier).ThenByDescending(s => s.IsRepresentative).ThenByDescending(s => s.IsVerified).ThenBy(s => s.ProviderCode == "usda" ? 1 : 0)
                .ThenBy(s => s.Name.Length).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id));
        // Custom: món user tự tạo (không từ khoá = các món tự tạo gần dùng)
        var custom = Group(Wants(FoodSearchSection.Custom), s => s.Source == FoodSource.UserCreated && s.CreatedByUserId == userId,
            r => r.OrderBy(Tier).ThenByDescending(LastOf).ThenBy(s => s.Name.Length).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id));
        // Branded: món có thương hiệu đã có trong DB (sản phẩm OFF từng quét mã vạch…); tắt "Branded" → rỗng, tắt "Open Food Facts" → bỏ món OFF
        var branded = Group(hasSearch && showBranded && Wants(FoodSearchSection.Branded),
            s => s.Source != FoodSource.UserCreated && !string.IsNullOrEmpty(s.Brand) && (showOff || s.Source != FoodSource.OpenFoodFacts),
            r => r.OrderBy(Tier).ThenByDescending(LastOf).ThenBy(s => s.Name.Length).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id));

        // Chi tiết (macro, khẩu phần mặc định, icon) chỉ cho các dòng của trang: MỘT truy vấn theo khoá chính + MỘT truy vấn icon riêng của user
        var pageIds = history.Page.Concat(common.Page).Concat(custom.Page).Concat(branded.Page).Select(s => s.Id).Distinct().ToList();
        var detail = pageIds.Count == 0 ? [] : (await _uow.Repository<FoodItem>().Query().Where(f => pageIds.Contains(f.Id)).Select(f => new
        {
            f.Id, f.Category, f.IconKey, f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
            Serving = f.Servings.OrderByDescending(s => s.IsDefault).Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).FirstOrDefault(),
        }).ToListAsync(ct)).ToDictionary(x => x.Id);
        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, pageIds, ct);

        FoodSearchGroup Map((List<Slim> Page, int Total) g) => new(g.Page.Select(s =>
        {
            var d = detail[s.Id];
            return new FoodItemModel(s.Id, s.Name, s.Brand, d.Category.ToString(), s.Source.ToString(), s.IsVerified, s.CreatedByUserId == userId,
                d.CaloriesPer100g, d.ProteinPer100g, d.CarbsPer100g, d.FatPer100g,
                d.Serving?.Id, d.Serving?.ServingName ?? "100 g", d.Serving?.WeightInGrams ?? 100m,
                LastOf(s), FoodEmoji.For(d.Category), FoodIconSupport.Effective(s.Id, d.IconKey, overrides));
        }).ToList(), g.Total);

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
