namespace Application;

public sealed class AddWaterCommandValidator : AbstractValidator<AddWaterCommand>
{
    public AddWaterCommandValidator()
    {
        RuleFor(x => x.AmountMl).InclusiveBetween(1m, 5000m);
        RuleFor(x => x.Source).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.LogDate)
            .NotInFuture()
            .When(x => x.LogDate.HasValue);
    }
}
