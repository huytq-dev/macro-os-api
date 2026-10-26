namespace Application;

public sealed class GetPlateNutrientsQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetPlateNutrientsQuery, PlateNutrientsResponse>
{
    public async Task<PlateNutrientsResponse> Handle(GetPlateNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await _clock.GetAsync(ct)).Today;
        var lines = request.Items ?? [];

        // Dòng có món/recipe nạp một lượt cho cả plate; dòng không có món là ước lượng/quick add
        var real = lines.Where(l => l.FoodItemId is not null || l.RecipeId is not null).ToList();
        var lookup = real.Count == 0
            ? null
            : await FoodLogEntryFactory.LoadAsync(_uow, userId, real.Select(l => new EntryLine(l.FoodItemId, l.RecipeId, l.FoodServingId, l.Quantity)).ToList(), ct);

        var parts = new List<IReadOnlyDictionary<string, decimal>>(lines.Count);
        foreach (var l in lines)
        {
            if (l.RecipeId is { } recipeId)
                parts.Add(NutritionCalculator.SnapshotAmounts(lookup!.RecipeProfiles[recipeId], l.Quantity));
            else if (l.FoodItemId is { } foodId)
            {
                var food = lookup!.Foods[foodId];
                var grams = FoodLogSupport.Grams(l.Quantity, FoodLogEntryFactory.ServingOf(food, l.FoodServingId));
                parts.Add(NutritionCalculator.SnapshotAmounts(lookup.FoodProfiles[foodId], NutritionCalculator.GramFactor(grams)));
            }
            else
                parts.Add(new Dictionary<string, decimal>
                {
                    ["energy_kcal"] = NutritionRounding.Round(l.Calories ?? 0, NutritionRounding.Macro),
                    ["protein_g"] = NutritionRounding.Round(l.Protein ?? 0, NutritionRounding.Macro),
                    ["carbs_g"] = NutritionRounding.Round(l.Carbs ?? 0, NutritionRounding.Macro),
                    ["fat_g"] = NutritionRounding.Round(l.Fat ?? 0, NutritionRounding.Macro),
                });
        }
        var plate = NutritionCalculator.SumAmounts(parts);

        // Ngày = đã log (snapshot entry) + plate; chất cả hai đều chưa có dữ liệu thì vắng mặt (amount null)
        var logged = await NutrientCatalog.SumsAsync(_uow, userId, date, date, ct);
        var day = new Dictionary<string, decimal>();
        foreach (var code in logged.Keys.Concat(plate.Keys).Distinct())
        {
            var loggedAmount = logged.GetValueOrDefault(code);
            if (loggedAmount is null && !plate.ContainsKey(code)) continue;
            day[code] = (loggedAmount ?? 0m) + plate.GetValueOrDefault(code);
        }

        return new PlateNutrientsResponse(
            date,
            await NutrientBreakdown.BuildAsync(_uow, userId, date, plate, ct),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, day, ct),
            await FoodLogSupport.EffectiveDayAsync(_uow, userId, date, lines.Count == 0 ? null : plate, ct));
    }
}
