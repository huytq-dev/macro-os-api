namespace Application;

public sealed class UpsertVisualBodyFatCommandValidator : AbstractValidator<UpsertVisualBodyFatCommand>
{
    public UpsertVisualBodyFatCommandValidator() => RuleFor(x => x.Percent).InclusiveBetween(2m, 70m).WithMessage(ValidationMessages.NumberRange);
}
