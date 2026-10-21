namespace Application;

public sealed class FoodServingInputValidator : AbstractValidator<FoodServingInput>
{
    public FoodServingInputValidator()
    {
        RuleFor(x => x.ServingName).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.WeightInGrams).InclusiveBetween(0.1m, 10000);
    }
}
