namespace Application;

/// <summary>Tính dinh dưỡng của recipe từ nguyên liệu (FoodNutrient /100 g × gram nguyên liệu). Thuần theo dữ liệu đã nạp, dùng chung log recipe và API recipe.</summary>
public static class RecipeCalculator
{
    /// <summary>Gram nguyên liệu: số lượng × gram của serving.</summary>
    public static decimal Grams(decimal quantity, FoodServing serving)
        => NutritionRounding.Round(NutritionCalculator.Grams(quantity, serving.WeightInGrams), NutritionRounding.Grams);

    /// <summary>Hồ sơ 1 khẩu phần của các recipe (đã Include Ingredients): hai truy vấn cho cả lô (món + FoodNutrient).</summary>
    public static async Task<Dictionary<Guid, Dictionary<string, NutrientValue>>> PerServingProfilesAsync(
        IUnitOfWork uow, IReadOnlyCollection<Recipe> recipes, CancellationToken ct)
    {
        var foodIds = recipes.SelectMany(r => r.Ingredients).Select(i => i.FoodItemId).Distinct().ToList();
        // Món nguyên liệu đã xoá mềm vẫn tính (recipe giữ nguyên dinh dưỡng khi log)
        var foods = await uow.Repository<FoodItem>().Query().IgnoreQueryFilters().Where(f => foodIds.Contains(f.Id)).ToListAsync(ct);
        var profiles = await NutrientSnapshot.LoadProfilesAsync(uow, foods, ct);

        return recipes.ToDictionary(r => r.Id, r =>
        {
            var parts = r.Ingredients
                .Where(i => profiles.ContainsKey(i.FoodItemId))
                .Select(i => ((IReadOnlyDictionary<string, NutrientValue>)profiles[i.FoodItemId], i.GramsNormalized ?? 0m))
                .ToList();
            return NutritionCalculator.PerServing(NutritionCalculator.SumProfiles(parts), r.ServingsCount);
        });
    }
}
