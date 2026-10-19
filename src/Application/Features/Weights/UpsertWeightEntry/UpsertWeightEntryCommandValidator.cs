namespace Application;

public sealed class UpsertWeightEntryCommandValidator : AbstractValidator<UpsertWeightEntryCommand>
{
    public UpsertWeightEntryCommandValidator()
    {
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20, 400).WithMessage(ValidationMessages.Range);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.Notes is not null);

        RuleFor(x => x.LogDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.LogDate.HasValue);
    }
}
