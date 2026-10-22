namespace Application;

public sealed class SetNutrientPinsCommandValidator : AbstractValidator<SetNutrientPinsCommand>
{
    public SetNutrientPinsCommandValidator()
    {
        RuleFor(x => x.Codes).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Codes).Must(c => c.Count <= 8).WithMessage(ValidationMessages.MaxPins8).When(x => x.Codes is not null);
        RuleForEach(x => x.Codes).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Codes)
            .Must(c => c.Select(s => s.Trim().ToLowerInvariant()).Distinct().Count() == c.Count)
            .WithMessage(ValidationMessages.DuplicateCode)
            .When(x => x.Codes is not null);
    }
}
