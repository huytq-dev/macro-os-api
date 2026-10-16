namespace Application;

/// <summary>Ghi FoodNutrition (15 trường /100 g) và servings vào FoodItem; dùng chung Create/Update custom food (sau này quick add).</summary>
public static class FoodNutritionApplier
{
    public static void Apply(FoodItem food, FoodNutrition n)
    {
        food.CaloriesPer100g     = n.CaloriesPer100g;
        food.ProteinPer100g      = n.ProteinPer100g;
        food.CarbsPer100g        = n.CarbsPer100g;
        food.FatPer100g          = n.FatPer100g;
        food.FiberPer100g        = n.FiberPer100g;
        food.SugarPer100g        = n.SugarPer100g;
        food.SodiumPer100g       = n.SodiumPer100g;
        food.PotassiumPer100g    = n.PotassiumPer100g;
        food.CalciumPer100g      = n.CalciumPer100g;
        food.IronPer100g         = n.IronPer100g;
        food.VitaminCPer100g     = n.VitaminCPer100g;
        food.VitaminDPer100g     = n.VitaminDPer100g;
        food.SaturatedFatPer100g = n.SaturatedFatPer100g;
        food.TransFatPer100g     = n.TransFatPer100g;
        food.CholesterolPer100g  = n.CholesterolPer100g;
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
