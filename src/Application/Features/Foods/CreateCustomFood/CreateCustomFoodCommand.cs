namespace Application;

/// <summary>UC-C03: custom food của user (Source = UserCreated). Servings rỗng → tự thêm "100 g".</summary>
public sealed record CreateCustomFoodCommand(
    string Name,
    string? Brand,
    FoodCategory Category,
    string? Barcode,
    FoodNutrition Nutrition,
    List<FoodServingInput>? Servings
) : IRequest<Guid>;

public sealed class CreateCustomFoodCommandValidator : AbstractValidator<CreateCustomFoodCommand>
{
    public CreateCustomFoodCommandValidator()
        => CustomFoodRules.Apply(this, x => x.Name, x => x.Brand, x => x.Barcode, x => x.Category, x => x.Nutrition, x => x.Servings);
}
