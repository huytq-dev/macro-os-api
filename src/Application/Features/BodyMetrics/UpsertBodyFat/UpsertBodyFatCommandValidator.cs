namespace Application;

public sealed class UpsertBodyFatCommandValidator : AbstractValidator<UpsertBodyFatCommand>
{
    public UpsertBodyFatCommandValidator()
    {
        RuleFor(x => x.Method).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.BodyFatPercent).InclusiveBetween(2m, 70m).WithMessage(ValidationMessages.NumberRange);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MeasuredDate)
            .NotInFuture()
            .When(x => x.MeasuredDate.HasValue);
    }
}
