namespace Application;

public sealed class UpdateCustomFoodCommandValidator : AbstractValidator<UpdateCustomFoodCommand>
{
    public UpdateCustomFoodCommandValidator()
        => CustomFoodRules.Apply(this, x => x.Name, x => x.Brand, x => x.Barcode, x => x.Category, x => x.Nutrition, x => x.Servings, x => x.Nutrients, x => x.IconKey);
}
