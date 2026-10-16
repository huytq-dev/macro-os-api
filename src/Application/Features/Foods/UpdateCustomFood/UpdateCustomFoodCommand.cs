namespace Application;

/// <summary>Sửa custom food của chính mình; Servings null/rỗng = giữ nguyên, có giá trị = thay toàn bộ.</summary>
public sealed record UpdateCustomFoodCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string Name,
    string? Brand,
    FoodCategory Category,
    string? Barcode,
    FoodNutrition Nutrition,
    List<FoodServingInput>? Servings
) : IRequest<Unit>;

public sealed class UpdateCustomFoodCommandValidator : AbstractValidator<UpdateCustomFoodCommand>
{
    public UpdateCustomFoodCommandValidator()
        => CustomFoodRules.Apply(this, x => x.Name, x => x.Brand, x => x.Barcode, x => x.Category, x => x.Nutrition, x => x.Servings);
}
