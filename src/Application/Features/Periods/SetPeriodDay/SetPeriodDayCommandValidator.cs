namespace Application;

public sealed class SetPeriodDayCommandValidator : AbstractValidator<SetPeriodDayCommand>
{
    public SetPeriodDayCommandValidator()
        => RuleFor(x => x.IsOnPeriod).NotNull().WithMessage(ValidationMessages.Required);
}
