namespace Application;

public sealed class CreatePeriodLogCommandValidator : AbstractValidator<CreatePeriodLogCommand>
{
    public CreatePeriodLogCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture);
        RuleFor(x => x).Must(x => x.EndDate is null || x.EndDate >= x.StartDate)
            .WithMessage("EndDate phải sau hoặc bằng StartDate.");
        RuleFor(x => x.Symptoms).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
    }
}
