namespace Application;

public sealed class AddEntriesBatchCommandValidator : AbstractValidator<AddEntriesBatchCommand>
{
    public const int MaxItems = 50;

    public AddEntriesBatchCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Source).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Items).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(i => i is null || i.Count <= MaxItems).WithMessage(ValidationMessages.MaxItemsPerBatch(MaxItems));
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i).Must(i => i.FoodItemId.HasValue != i.RecipeId.HasValue)
                .WithName("FoodItemId").WithMessage(ValidationMessages.EntryLineFoodOrRecipeExactlyOne);
            item.RuleFor(i => i.FoodServingId).Null().When(i => i.RecipeId.HasValue).WithMessage(ValidationMessages.RecipeLineHasNoServing);
            item.RuleFor(i => i.Quantity).InclusiveBetween(0.01m, 10000);
            item.RuleFor(i => i.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        });
    }
}
