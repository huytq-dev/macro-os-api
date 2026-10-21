namespace Application;

public sealed class UpsertStepEntryCommandValidator : AbstractValidator<UpsertStepEntryCommand>
{
    public UpsertStepEntryCommandValidator()
    {
        RuleFor(x => x.Steps).InclusiveBetween(0, 200_000).WithMessage(ValidationMessages.Range);

        RuleFor(x => x.LogDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.LogDate.HasValue);
    }
}
