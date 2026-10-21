namespace Application;

public sealed class UpdateFoodLogEntryCommandValidator : AbstractValidator<UpdateFoodLogEntryCommand>
{
    public UpdateFoodLogEntryCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
    }
}
