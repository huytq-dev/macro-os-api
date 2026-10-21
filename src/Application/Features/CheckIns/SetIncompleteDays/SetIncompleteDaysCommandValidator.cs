namespace Application;

public sealed class SetIncompleteDaysCommandValidator : AbstractValidator<SetIncompleteDaysCommand>
{
    public SetIncompleteDaysCommandValidator()
        => RuleFor(x => x.Dates).NotNull().WithMessage(ValidationMessages.Required)
            .Must(d => d is null || d.Count <= 7).WithMessage(ValidationMessages.MaxLength);
}
