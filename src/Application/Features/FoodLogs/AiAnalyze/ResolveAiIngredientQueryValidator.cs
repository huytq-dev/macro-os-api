namespace Application;

public sealed class ResolveAiIngredientQueryValidator : AbstractValidator<ResolveAiIngredientQuery>
{
    public ResolveAiIngredientQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Grams).GreaterThan(0).LessThanOrEqualTo(5000);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Unit).Must(u => AiIngredientResolution.NormalizeUnit(u) is not null)
            .WithMessage(ValidationMessages.InvalidEnumValue).When(x => !string.IsNullOrWhiteSpace(x.Unit));
        RuleFor(x => x.State).Must(FoodPreparationState.IsValid).WithMessage(ValidationMessages.InvalidEnumValue);
    }
}
