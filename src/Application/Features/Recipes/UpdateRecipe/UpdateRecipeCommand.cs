using System.Text.Json;

namespace Application;

/// <summary>WP4: sửa recipe (gửi đủ object, nguyên liệu = thay toàn bộ). Sinh bản mới; entry đã log giữ snapshot cũ.</summary>
public sealed record UpdateRecipeCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
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
) : IRequest<Unit>;
