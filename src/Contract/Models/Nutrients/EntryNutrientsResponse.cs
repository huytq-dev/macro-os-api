namespace Contract;

/// <summary>Mọi chất của một entry đã log (GET /food-logs/entries/{id}/nutrients), đọc từ snapshot lúc log.</summary>
public sealed record EntryNutrientsResponse(
    Guid EntryId,
    string FoodName,
    DateOnly Date,
    decimal Quantity,
    IReadOnlyList<NutrientAmountModel> Items
);
