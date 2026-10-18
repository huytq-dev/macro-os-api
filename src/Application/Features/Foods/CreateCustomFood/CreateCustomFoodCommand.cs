namespace Application;

/// <summary>UC-C03: custom food của user (Source = UserCreated). Servings rỗng → tự thêm "100 g". Nutrients: thêm vi chất theo mã (/100 g), ghi đè trường cùng mã của Nutrition.</summary>
public sealed record CreateCustomFoodCommand(
    string Name,
    string? Brand,
    FoodCategory Category,
    string? Barcode,
    FoodNutrition Nutrition,
    List<FoodServingInput>? Servings,
    List<FoodNutrientInput>? Nutrients = null,
    string? IconKey = null
) : IRequest<Guid>;

public sealed class CreateCustomFoodCommandValidator : AbstractValidator<CreateCustomFoodCommand>
{
    public CreateCustomFoodCommandValidator()
        => CustomFoodRules.Apply(this, x => x.Name, x => x.Brand, x => x.Barcode, x => x.Category, x => x.Nutrition, x => x.Servings, x => x.Nutrients, x => x.IconKey);
}
