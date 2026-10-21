namespace Application;

public sealed class ChangeCheckInWeekdayCommandValidator : AbstractValidator<ChangeCheckInWeekdayCommand>
{
    public ChangeCheckInWeekdayCommandValidator()
        => RuleFor(x => x.CheckInWeekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
}
