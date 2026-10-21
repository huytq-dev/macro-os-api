using System.Text.Json;

namespace Application;

public sealed class GetRecipesQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetRecipesQuery, QueryResult<RecipeSummaryModel>>
{
    public async Task<QueryResult<RecipeSummaryModel>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var top = Math.Clamp(request.Top, 1, AppConstants.MaxPageSize);
        var skip = Math.Max(request.Skip, 0);

        var q = _uow.Repository<Recipe>().Query().Where(r => r.CreatedByUserId == userId && !r.IsArchived);
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) q = q.Where(r => r.Name.Contains(search));

        var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(r => r.Name).ThenBy(r => r.Id).Skip(skip).Take(top)
            .Select(r => new
            {
                r.Id, r.Name, r.ImageUrl, r.ServingsCount, r.MealType,
                r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.CreatedAt, r.ModifiedAt, r.IconKey,
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new RecipeSummaryModel(
            r.Id, r.Name, r.ImageUrl, r.ServingsCount, r.MealType.ToString(),
            RecipeWriter.PerServing(r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.ServingsCount),
            r.ModifiedAt ?? r.CreatedAt, r.IconKey)).ToList();
        return new QueryResult<RecipeSummaryModel> { Items = items, TotalCount = total };
    }
}
