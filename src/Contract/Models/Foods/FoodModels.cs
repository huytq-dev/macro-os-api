namespace Contract;

/// <summary>
/// Dinh dưỡng trên 100 g: kcal và g cho macro; mg cho sodium/potassium/calcium/iron/vitaminC/cholesterol; µg cho vitaminD.
/// Bốn macro lưu ở FoodItem, 11 trường còn lại lưu như mọi chất khác ở FoodNutrient (đọc/ghi qua đây chỉ để giữ contract cũ).
/// </summary>
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

/// <summary>Một chất của món, trên 100 g, theo <c>NutrientDefinition.Code</c> và đơn vị của chất đó (xem GET /nutrients?all=true).</summary>
public sealed record FoodNutrientInput(string Code, decimal AmountPer100g);

public sealed record FoodNutrientValue(string Code, decimal AmountPer100g);

public sealed record FoodServingInput(string ServingName, decimal WeightInGrams, bool IsDefault);

/// <param name="Label">Tên hiển thị cho chip đơn vị: ServingName bỏ số "1 " đầu ("1 large - 3 1/4\" diameter" → "large - 3 1/4\" diameter").</param>
public sealed record FoodServingModel(Guid Id, string ServingName, decimal WeightInGrams, bool IsDefault, string Label = "");

/// <summary>iconKey hiệu lực của user hiện tại sau khi đổi icon (null = chưa có icon, FE dùng emoji).</summary>
public sealed record FoodIconResponse(string? IconKey);

/// <summary>Dòng kết quả tìm kiếm (phẳng).</summary>
/// <param name="Emoji">Emoji đại diện món suy từ nhóm món (cùng quy tắc với entry food-log); null cho nhóm Other.</param>
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
    DateTimeOffset? LastLoggedAt,
    string? Emoji,
    string? IconKey = null
);

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
