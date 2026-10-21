namespace Application;

public sealed class PreviewProgramQueryValidator : AbstractValidator<PreviewProgramQuery>
{
    public PreviewProgramQueryValidator()
    {
        RuleFor(x => x.GoalType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.DietStyle).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.ProteinLevel).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.CalorieFloor).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.TrainingType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.ShiftMode).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.WeeklyRatePercent).InclusiveBetween(0m, 1.5m).WithMessage(ValidationMessages.Range);
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range).When(x => x.TargetWeightKg.HasValue);
        RuleFor(x => x.ShiftPercent).InclusiveBetween(5m, 30m).WithMessage(ValidationMessages.Range).When(x => x.ShiftPercent.HasValue);
        RuleFor(x => x.ManualTdeeKcal).InclusiveBetween(1000, 6000).WithMessage(ValidationMessages.Range).When(x => x.ManualTdeeKcal.HasValue);
    }
}
