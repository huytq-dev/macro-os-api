namespace Application;

public sealed class UpsertStepEntryCommandValidator : AbstractValidator<UpsertStepEntryCommand>
{
    public UpsertStepEntryCommandValidator()
    {
        RuleFor(x => x.Steps).InclusiveBetween(0, 200_000).WithMessage(ValidationMessages.NumberRange);

        RuleFor(x => x.LogDate)
            .NotInFuture()
            .When(x => x.LogDate.HasValue);
    }
}
