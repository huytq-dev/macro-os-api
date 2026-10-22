namespace Application;

public sealed class SetExpenditureModeCommandValidator : AbstractValidator<SetExpenditureModeCommand>
{
    public SetExpenditureModeCommandValidator()
    {
        RuleFor(x => x.Mode).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.StaticTdeeKcal).NotNull().WithMessage(ValidationMessages.Required).When(x => x.Mode == ExpenditureMode.Static);
        RuleFor(x => x.StaticTdeeKcal).InclusiveBetween(1000, 6000).WithMessage(ValidationMessages.NumberRange).When(x => x.StaticTdeeKcal.HasValue);
        RuleFor(x => x.StaticTdeeKcal).Null().WithMessage(ValidationMessages.InvalidFormat).When(x => x.Mode == ExpenditureMode.Dynamic);
    }
}
