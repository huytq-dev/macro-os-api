namespace Application;

/// <summary>Danh sách preset yêu thích của người dùng theo thứ tự tự sắp xếp.</summary>
public sealed record GetFavoritesQuery : IRequest<IReadOnlyList<FavoriteFoodModel>>;

public sealed class GetFavoritesQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetFavoritesQuery, IReadOnlyList<FavoriteFoodModel>>
{
    public async Task<IReadOnlyList<FavoriteFoodModel>> Handle(GetFavoritesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        // Chỉ lấy cột cần dùng (không Include cả FoodItem/FoodServing); tính macro trong bộ nhớ cho đủ chính xác.
        var rows = await _uow.Repository<FoodFavorite>().Query()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.FoodItemId,
                f.FoodItem.Name,
                f.FoodItem.Brand,
                f.FoodItem.Category,
                f.FoodItem.CaloriesPer100g,
                f.FoodItem.ProteinPer100g,
                f.FoodItem.CarbsPer100g,
                f.FoodItem.FatPer100g,
                f.FoodServingId,
                ServingName = f.FoodServing != null ? f.FoodServing.ServingName : null,
                ServingGrams = f.FoodServing != null ? (decimal?)f.FoodServing.WeightInGrams : null,
                f.Quantity,
                f.SortOrder,
            })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            var grams = r.Quantity * (r.ServingGrams ?? 1m);
            var k = grams / 100m;
            return new FavoriteFoodModel(
                r.Id, r.FoodItemId, r.Name, r.Brand, r.Category.ToString(),
                r.FoodServingId, r.ServingName ?? "gram", r.Quantity, Math.Round(grams, 1),
                Math.Round(r.CaloriesPer100g * k, 0), Math.Round(r.ProteinPer100g * k, 1),
                Math.Round(r.CarbsPer100g * k, 1), Math.Round(r.FatPer100g * k, 1), r.SortOrder);
        }).ToList();
    }
}
