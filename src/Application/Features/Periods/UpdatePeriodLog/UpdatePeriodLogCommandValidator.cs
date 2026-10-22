namespace Application;

public sealed class UpdatePeriodLogCommandValidator : AbstractValidator<UpdatePeriodLogCommand>
{
    public UpdatePeriodLogCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .NotInFuture();
        RuleFor(x => x).Must(x => x.EndDate is null || x.EndDate >= x.StartDate)
            .WithMessage(ValidationMessages.EndDateBeforeStart);
        RuleFor(x => x.Symptoms).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
    }
}
