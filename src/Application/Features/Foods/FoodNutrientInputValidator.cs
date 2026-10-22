namespace Application;

public sealed class FoodNutrientInputValidator : AbstractValidator<FoodNutrientInput>
{
    public FoodNutrientInputValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(50).WithMessage(ValidationMessages.MaxLength)
            .Must(c => !NutrientSnapshot.FoodColumnCodes.Contains(c)).WithMessage(ValidationMessages.MacroInNutrientInput);
        RuleFor(x => x.AmountPer100g).InclusiveBetween(0, 1_000_000m);
    }
}
