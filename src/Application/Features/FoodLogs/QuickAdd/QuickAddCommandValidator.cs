namespace Application;

public sealed class QuickAddCommandValidator : AbstractValidator<QuickAddCommand>
{
    public QuickAddCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Name).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000);
        RuleFor(x => x.Protein).InclusiveBetween(0, 1000);
        RuleFor(x => x.Carbs).InclusiveBetween(0, 1000);
        RuleFor(x => x.Fat).InclusiveBetween(0, 1000);
    }
}
