namespace Application;

public sealed class UpsertWeightEntryCommandValidator : AbstractValidator<UpsertWeightEntryCommand>
{
    public UpsertWeightEntryCommandValidator()
    {
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20, 400).WithMessage(ValidationMessages.NumberRange);

        RuleFor(x => x.BodyFatPercent).InclusiveBetween(2m, 70m).WithMessage(ValidationMessages.NumberRange).When(x => x.BodyFatPercent.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.Notes is not null);

        RuleFor(x => x.LogDate)
            .NotInFuture()
            .When(x => x.LogDate.HasValue);
    }
}
