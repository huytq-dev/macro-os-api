using System.Text.Json;

namespace Application;

public sealed class GetRecipeNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetRecipeNutrientsQuery, RecipeNutrientsResponse>
{
    public async Task<RecipeNutrientsResponse> Handle(GetRecipeNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, request.Ingredients.Select(i => new EntryLine(i.FoodItemId, null, i.FoodServingId, i.Quantity)).ToList(), ct);

        decimal totalGrams = 0;
        var parts = new List<IReadOnlyDictionary<string, decimal>>(request.Ingredients.Count);
        foreach (var i in request.Ingredients)
        {
            var food = lookup.Foods[i.FoodItemId];
            var grams = FoodLogSupport.Grams(i.Quantity, FoodLogEntryFactory.ServingOf(food, i.FoodServingId));
            totalGrams += grams;
            parts.Add(NutritionCalculator.SnapshotAmounts(lookup.FoodProfiles[food.Id], NutritionCalculator.GramFactor(grams)));
        }
        var total = NutritionCalculator.SumAmounts(parts);
        var perServing = total.ToDictionary(kv => kv.Key, kv => kv.Value / request.ServingsCount);

        var date = request.Date ?? (await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct)).Date;
        return new RecipeNutrientsResponse(
            date, NutritionRounding.Round(totalGrams, NutritionRounding.Grams), request.ServingsCount,
            Macros(total), Macros(perServing),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, total, ct),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, perServing, ct));
    }

    private static RecipeMacros Macros(IReadOnlyDictionary<string, decimal> amounts) => new(
        NutritionRounding.Round(amounts.GetValueOrDefault("energy_kcal"), NutritionRounding.Macro),
        NutritionRounding.Round(amounts.GetValueOrDefault("protein_g"), NutritionRounding.Macro),
        NutritionRounding.Round(amounts.GetValueOrDefault("carbs_g"), NutritionRounding.Macro),
        NutritionRounding.Round(amounts.GetValueOrDefault("fat_g"), NutritionRounding.Macro));
}
