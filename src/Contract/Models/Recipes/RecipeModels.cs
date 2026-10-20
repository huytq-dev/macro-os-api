namespace Contract;

/// <summary>
/// Nguyên liệu: món + khẩu phần + số lượng (Quantity × gram của khẩu phần = lượng thực). FoodServingId null = Quantity là số GRAM (như log theo gram ở POST /food-logs/entries):
/// lượng thực giữ đúng số gram, khẩu phần lưu là khẩu phần mặc định của món. Dùng khi tạo recipe từ các entry đã log theo gram (Multi-Select → Create Recipe).
/// </summary>
public sealed record RecipeIngredientInput(Guid FoodItemId, Guid? FoodServingId, decimal Quantity, string? Notes);

/// <summary>Một bước làm khi tạo/sửa recipe: Order thiếu thì theo vị trí trong mảng; Text ≤ 300 ký tự.</summary>
public sealed record RecipeStepInput(int? Order, string Text);

/// <param name="Order">Thứ tự 1..n.</param>
public sealed record RecipeStepModel(int Order, string Text);

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
    DateTimeOffset UpdatedAt,
    string? IconKey = null
);

/// <param name="Instructions">Cách làm dạng văn bản tự do, lưu ở bản (RecipeVersion) hiện hành (cũ; Create Recipe mới dùng Steps).</param>
/// <param name="SourceUrl">Ô Link của Create Recipe.</param>
/// <param name="Steps">Các bước làm theo thứ tự (Order 1..n).</param>
/// <param name="IconKey">Icon user chọn; null = FE xếp chồng icon nguyên liệu.</param>
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
    IReadOnlyList<RecipeIngredientModel> Ingredients,
    IReadOnlyList<RecipeStepModel>? Steps = null,
    string? IconKey = null
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

/// <summary>Dinh dưỡng của recipe đang soạn (chưa lưu): tổng và theo khẩu phần (tổng ÷ ServingsCount), mỗi bên đủ danh mục chất + % target của ngày Date.</summary>
public sealed record RecipeNutrientsResponse(
    DateOnly Date,
    decimal TotalGrams,
    int ServingsCount,
    RecipeMacros Total,
    RecipeMacros PerServing,
    IReadOnlyList<NutrientAmountModel> TotalNutrients,
    IReadOnlyList<NutrientAmountModel> PerServingNutrients
);

/// <param name="EntryId">Entry food log vừa tạo khi "Create &amp; Add".</param>
public sealed record RecipeCreatedResponse(Guid RecipeId, Guid? EntryId);
