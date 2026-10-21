using System.Text.Json;

namespace Application;

public sealed class GetRecipeQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetRecipeQuery, RecipeResponse>
{
    public async Task<RecipeResponse> Handle(GetRecipeQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var r = await _uow.Repository<Recipe>().Query()
            .Where(x => x.Id == request.Id && x.CreatedByUserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Recipe", request.Id));

        var version = r.CurrentVersionId is { } vid
            ? await _uow.Repository<RecipeVersion>().Query().Where(v => v.Id == vid).Select(v => new { v.Instructions, v.CookedYieldGrams }).FirstOrDefaultAsync(ct)
            : null;

        var ingredients = await _uow.Repository<RecipeIngredient>().Query()
            .Where(i => i.RecipeId == r.Id).OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .ToListAsync(ct);
        // Món nguyên liệu đã xoá mềm vẫn hiện (recipe giữ nguyên dinh dưỡng)
        var foodIds = ingredients.Select(i => i.FoodItemId).Distinct().ToList();
        var foods = await _uow.Repository<FoodItem>().Query().IgnoreQueryFilters()
            .Where(f => foodIds.Contains(f.Id))
            .Select(f => new { f.Id, f.Name, f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g })
            .ToDictionaryAsync(f => f.Id, ct);
        var servingIds = ingredients.Select(i => i.FoodServingId).Distinct().ToList();
        var servings = await _uow.Repository<FoodServing>().Query()
            .Where(s => servingIds.Contains(s.Id)).Select(s => new { s.Id, s.ServingName })
            .ToDictionaryAsync(s => s.Id, s => s.ServingName, ct);

        var models = ingredients.Select(i =>
        {
            var f = foods[i.FoodItemId];
            var grams = i.GramsNormalized ?? 0m;
            return new RecipeIngredientModel(i.Id, i.FoodItemId, f.Name, i.FoodServingId, servings.GetValueOrDefault(i.FoodServingId, "100 g"),
                i.Quantity, grams, NutritionCalculator.Scale(f.CaloriesPer100g, grams), NutritionCalculator.Scale(f.ProteinPer100g, grams),
                NutritionCalculator.Scale(f.CarbsPer100g, grams), NutritionCalculator.Scale(f.FatPer100g, grams), i.Notes);
        }).ToList();

        return new RecipeResponse(
            r.Id, r.Name, r.Description, r.ImageUrl, r.ServingsCount, r.PrepTimeMinutes, r.CookTimeMinutes, r.MealType.ToString(), r.SourceUrl,
            version?.Instructions, version?.CookedYieldGrams, models.Sum(m => m.Grams),
            new RecipeMacros(r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat),
            RecipeWriter.PerServing(r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.ServingsCount),
            models, RecipeSteps.Parse(r.StepsJson), r.IconKey);
    }
}
