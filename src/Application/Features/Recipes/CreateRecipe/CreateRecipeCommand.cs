using System.Text.Json;

namespace Application;

/// <summary>WP4: tạo recipe (MealType thiếu → Lunch). Dinh dưỡng tính từ nguyên liệu; mỗi lần tạo/sửa sinh một bản RecipeVersion.</summary>
public sealed record CreateRecipeCommand(
    string Name,
    string? Description,
    string? ImageUrl,
    int ServingsCount,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    MealType? MealType,
    string? SourceUrl,
    string? Instructions,
    decimal? CookedYieldGrams,
    List<RecipeIngredientInput> Ingredients,
    List<RecipeStepInput>? Steps = null,
    string? IconKey = null
) : IRequest<Guid>;
