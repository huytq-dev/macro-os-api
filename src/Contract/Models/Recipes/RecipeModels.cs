namespace Contract;

/// <summary>Nguyên liệu: món + khẩu phần + số lượng (Quantity × gram của khẩu phần = lượng thực).</summary>
public sealed record RecipeIngredientInput(Guid FoodItemId, Guid FoodServingId, decimal Quantity, string? Notes);

public sealed record RecipeMacros(decimal Calories, decimal Protein, decimal Carbs, decimal Fat);

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

/// <param name="PerServing">Tổng cả recipe chia số khẩu phần.</param>
public sealed record RecipeSummaryModel(
    Guid Id,
    string Name,
    string? ImageUrl,
    int ServingsCount,
    string MealType,
    RecipeMacros PerServing,
    DateTimeOffset UpdatedAt
);

/// <param name="Instructions">Cách làm, lưu ở bản (RecipeVersion) hiện hành.</param>
/// <param name="CookedYieldGrams">Khối lượng sau nấu (nếu nhập); null = chưa biết.</param>
/// <param name="TotalGrams">Tổng khối lượng nguyên liệu sống.</param>
public sealed record RecipeResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int ServingsCount,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string MealType,
    string? SourceUrl,
    string? Instructions,
    decimal? CookedYieldGrams,
    decimal TotalGrams,
    RecipeMacros Total,
    RecipeMacros PerServing,
    IReadOnlyList<RecipeIngredientModel> Ingredients
);

/// <summary>Dòng thư viện của tôi: món tự tạo hoặc recipe.</summary>
/// <param name="Kind">Food | Recipe.</param>
/// <param name="Calories">Theo khẩu phần mặc định (món) hoặc 1 khẩu phần (recipe) — cùng cho Protein/Carbs/Fat.</param>
/// <param name="DefaultServingId">Chỉ có với món; recipe log theo số khẩu phần, không có serving.</param>
public sealed record LibraryItemModel(
    string Kind,
    Guid Id,
    string Name,
    string? Brand,
    string? Emoji,
    Guid? DefaultServingId,
    string ServingName,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    DateTimeOffset UpdatedAt,
    string? IconKey = null
);
