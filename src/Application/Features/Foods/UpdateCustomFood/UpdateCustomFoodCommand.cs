namespace Application;

/// <summary>
/// Sửa custom food của chính mình; Servings null/rỗng = giữ nguyên, có giá trị = thay toàn bộ.
/// Nutrients null = giữ vi chất đã có ngoài 11 chất của Nutrition; có giá trị = thay toàn bộ chất ngoài macro (gồm cả 11 chất của Nutrition).
/// </summary>
public sealed record UpdateCustomFoodCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string Name,
    string? Brand,
    FoodCategory Category,
    string? Barcode,
    FoodNutrition Nutrition,
    List<FoodServingInput>? Servings,
    List<FoodNutrientInput>? Nutrients = null,
    string? IconKey = null
) : IRequest<Unit>;
