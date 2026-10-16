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

        var q = _uow.Repository<FoodItem>().Query()
            .Where(f => f.Source != FoodSource.QuickAdd);

        q = request.Scope switch
        {
            FoodScope.Mine   => q.Where(f => f.CreatedByUserId == userId),
            FoodScope.System => q.Where(f => f.Source != FoodSource.UserCreated),
            _                => q.Where(f => f.Source != FoodSource.UserCreated || f.CreatedByUserId == userId),
        };

        if (request.Category.HasValue)
            q = q.Where(f => f.Category == request.Category.Value);

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(f => f.Name.Contains(search) || (f.Brand != null && f.Brand.Contains(search)));

        // ponytail: LIKE %x% trên index Name; full-text/ranking khi dữ liệu USDA vào
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(f => f.Name).ThenBy(f => f.Id)
            .Skip(skip).Take(top)
            .Select(f => new FoodItemModel(
                f.Id, f.Name, f.Brand, f.Category.ToString(), f.Source.ToString(), f.IsVerified,
                f.CreatedByUserId == userId,
                f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g))
            .ToListAsync(ct);

        return new QueryResult<FoodItemModel> { Items = items, TotalCount = total };
    }
}
