namespace Contract;

public sealed record RecipeIngredientModel(
    Guid Id,
    Guid FoodItemId,
    string FoodName,
    Guid FoodServingId,
    string ServingName,
    decimal Quantity,
    decimal Grams,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    string? Notes
);
