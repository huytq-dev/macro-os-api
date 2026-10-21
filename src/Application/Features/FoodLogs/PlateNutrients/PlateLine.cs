namespace Application;

/// <summary>
/// Một dòng của plate. Món: FoodItemId + FoodServingId (tuỳ chọn) + Quantity; FoodServingId null thì Quantity là số gram, đúng như log thật (POST /food-logs/entries).
/// Recipe: RecipeId + Quantity (số khẩu phần), không có FoodServingId. Ước lượng/quick add (không có món): Name + tổng kcal/P/C/F, không có vi chất.
/// </summary>
public sealed record PlateLine(
    Guid? FoodItemId,
    Guid? RecipeId,
    Guid? FoodServingId,
    decimal Quantity = 1,
    string? Name = null,
    decimal? Calories = null,
    decimal? Protein = null,
    decimal? Carbs = null,
    decimal? Fat = null
);
