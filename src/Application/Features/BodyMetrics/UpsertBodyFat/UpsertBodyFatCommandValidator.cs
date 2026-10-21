namespace Application;

public sealed class UpsertBodyFatCommandValidator : AbstractValidator<UpsertBodyFatCommand>
{
    public UpsertBodyFatCommandValidator()
    {
        RuleFor(x => x.Method).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.BodyFatPercent).InclusiveBetween(2m, 70m).WithMessage(ValidationMessages.Range);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MeasuredDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.MeasuredDate.HasValue);
    }
}
