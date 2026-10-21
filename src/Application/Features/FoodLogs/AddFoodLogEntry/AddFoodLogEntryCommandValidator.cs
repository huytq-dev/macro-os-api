namespace Application;

public sealed class AddFoodLogEntryCommandValidator : AbstractValidator<AddFoodLogEntryCommand>
{
    public AddFoodLogEntryCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x).Must(x => x.FoodItemId.HasValue != x.RecipeId.HasValue)
            .WithName("FoodItemId").WithMessage("Cần đúng một trong FoodItemId hoặc RecipeId.");
        RuleFor(x => x.FoodServingId).Null().When(x => x.RecipeId.HasValue).WithMessage("Recipe không có khẩu phần riêng.");
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
    }
}
