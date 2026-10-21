namespace Contract;

/// <summary>Mọi chất của một món theo khẩu phần (GET /foods/{id}/nutrients). Có thêm <c>carbs_net_g</c> (carbs − fiber) khi biết carbs.</summary>
public sealed record FoodNutrientsResponse(
    Guid FoodItemId,
    string FoodName,
    Guid? ServingId,
    string ServingName,
    decimal Quantity,
    decimal Grams,
    DateOnly Date,
    IReadOnlyList<NutrientAmountModel> Items
);
