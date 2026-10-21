namespace Application;

public sealed class PreviewEditGoalQueryValidator : AbstractValidator<PreviewEditGoalQuery>
{
    public PreviewEditGoalQueryValidator()
    {
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range).When(x => x.TargetWeightKg.HasValue);
        RuleFor(x => x.WeeklyRatePercent).InclusiveBetween(0m, 1.5m).WithMessage(ValidationMessages.Range).When(x => x.WeeklyRatePercent.HasValue);
    }
}
