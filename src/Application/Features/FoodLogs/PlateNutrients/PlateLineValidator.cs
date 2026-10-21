namespace Application;

public sealed class PlateLineValidator : AbstractValidator<PlateLine>
{
    public PlateLineValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000)
            .When(x => x.FoodItemId is not null || x.RecipeId is not null);
        RuleFor(x => x.FoodServingId).Null().WithMessage("Recipe không có khẩu phần riêng (FoodServingId phải để trống).")
            .When(x => x.RecipeId is not null);
        RuleFor(x => x.Name).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000).When(x => x.Calories is not null);
        RuleFor(x => x.Protein).InclusiveBetween(0, 1000).When(x => x.Protein is not null);
        RuleFor(x => x.Carbs).InclusiveBetween(0, 1000).When(x => x.Carbs is not null);
        RuleFor(x => x.Fat).InclusiveBetween(0, 1000).When(x => x.Fat is not null);
    }
}
