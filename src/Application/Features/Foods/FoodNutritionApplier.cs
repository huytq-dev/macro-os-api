namespace Application;

/// <summary>Ghi macro (FoodNutrition) và servings vào FoodItem; dùng chung Create/Update custom food (sau này quick add).</summary>
public static class FoodNutritionApplier
{
    /// <summary>Ghi 4 macro vào cột FoodItem; 11 chất còn lại của <paramref name="n"/> ghi qua <see cref="FoodNutrientStore"/>.</summary>
    public static void Apply(FoodItem food, FoodNutrition n)
    {
        food.CaloriesPer100g = n.CaloriesPer100g;
        food.ProteinPer100g  = n.ProteinPer100g;
        food.CarbsPer100g    = n.CarbsPer100g;
        food.FatPer100g      = n.FatPer100g;
    }

    /// <summary>Thay toàn bộ serving; đảm bảo đúng 1 default (serving đầu nếu không có).</summary>
    public static void ReplaceServings(FoodItem food, IReadOnlyList<FoodServingInput> inputs)
    {
        food.Servings.Clear();
        var defaultIdx = Math.Max(0, inputs.ToList().FindIndex(s => s.IsDefault));
        for (var i = 0; i < inputs.Count; i++)
            food.Servings.Add(new FoodServing
            {
                Id = Guid.NewGuid(),
                FoodItemId = food.Id,
                ServingName = inputs[i].ServingName.Trim(),
                WeightInGrams = inputs[i].WeightInGrams,
                IsDefault = i == defaultIdx,
            });
    }
}
