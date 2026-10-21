namespace Contract;

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
