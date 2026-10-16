namespace Contract;

/// <summary>Dinh dưỡng trên 100 g: kcal và g cho macro; mg cho sodium/potassium/calcium/iron/vitaminC/cholesterol; µg cho vitaminD.</summary>
public sealed record FoodNutrition(
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    decimal? FiberPer100g = null,
    decimal? SugarPer100g = null,
    decimal? SodiumPer100g = null,
    decimal? PotassiumPer100g = null,
    decimal? CalciumPer100g = null,
    decimal? IronPer100g = null,
    decimal? VitaminCPer100g = null,
    decimal? VitaminDPer100g = null,
    decimal? SaturatedFatPer100g = null,
    decimal? TransFatPer100g = null,
    decimal? CholesterolPer100g = null
);

public sealed record FoodServingInput(string ServingName, decimal WeightInGrams, bool IsDefault);

public sealed record FoodServingModel(Guid Id, string ServingName, decimal WeightInGrams, bool IsDefault);

/// <summary>Dòng kết quả tìm kiếm (phẳng).</summary>
public sealed record FoodItemModel(
    Guid Id,
    string Name,
    string? Brand,
    string Category,
    string Source,
    bool IsVerified,
    bool IsMine,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    Guid? DefaultServingId,
    string DefaultServingName,
    decimal DefaultServingGrams,
    DateTimeOffset? LastLoggedAt
);

public sealed record FoodItemResponse(
    Guid Id,
    string Name,
    string? Brand,
    string? Barcode,
    string Category,
    string Source,
    bool IsVerified,
    bool IsMine,
    string? ImageUrl,
    FoodNutrition Nutrition,
    IReadOnlyList<FoodServingModel> Servings
);
