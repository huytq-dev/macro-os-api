namespace Contract;

/// <param name="Nutrition">Contract cũ: 4 macro + 11 chất hay dùng.</param>
/// <param name="Nutrients">Mọi chất ngoài 4 macro của món (/100 g), kể cả 11 chất trong <c>Nutrition</c>.</param>
/// <param name="GramsPerMl">Khối lượng riêng g/ml (lưu trên món, hoặc suy từ khẩu phần thể tích cup/tbsp/tsp/fl oz/ml); null = chưa biết → FE chỉ bật đơn vị khối lượng và khẩu phần của món. lb luôn dùng được.</param>
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
    IReadOnlyList<FoodServingModel> Servings,
    IReadOnlyList<FoodNutrientValue> Nutrients,
    IReadOnlyDictionary<string, decimal?> NutrientsPer100g,
    string? Emoji = null,
    string? IconKey = null,
    decimal? GramsPerMl = null
);
