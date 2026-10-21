namespace Application;

public sealed class ConfirmAiScanCommandValidator : AbstractValidator<ConfirmAiScanCommand>
{
    public ConfirmAiScanCommandValidator()
    {
        RuleFor(x => x.ScanId).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Ingredients).NotNull().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count <= 50).WithMessage(ValidationMessages.AiConfirmMaxIngredients);
        RuleForEach(x => x.Ingredients).ChildRules(i =>
        {
            i.RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
            i.RuleFor(x => x.State).Must(FoodPreparationState.IsValid).WithMessage(ValidationMessages.InvalidEnumValue);
            i.RuleFor(x => x).Must(x => x.FoodItemId.HasValue || x.CanonicalFoodId.HasValue)
                .WithName("FoodItemId").WithMessage(ValidationMessages.AiConfirmNeedsFood);
        });
    }
}
