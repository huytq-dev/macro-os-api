namespace Application;

public sealed class UpdateProgramGoalCommandValidator : AbstractValidator<UpdateProgramGoalCommand>
{
    public UpdateProgramGoalCommandValidator()
    {
        RuleFor(x => x.GoalType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.GoalType.HasValue);
        RuleFor(x => x.DietStyle).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.DietStyle.HasValue);
        RuleFor(x => x.ProteinLevel).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ProteinLevel.HasValue);
        RuleFor(x => x.CheckInWeekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.CheckInWeekday.HasValue);

        RuleFor(x => x.WeeklyWeightChangeTargetPercent)
            .InclusiveBetween(-1.5m, 1.5m).WithMessage(ValidationMessages.Range)
            .When(x => x.WeeklyWeightChangeTargetPercent.HasValue);

        RuleFor(x => x.FixedProteinGrams)
            .InclusiveBetween(30, 400).WithMessage(ValidationMessages.Range)
            .When(x => x.FixedProteinGrams.HasValue);

        RuleFor(x => x.CalorieFloorKcal)
            .InclusiveBetween(800, 3000).WithMessage(ValidationMessages.Range)
            .When(x => x.CalorieFloorKcal.HasValue);
    }
}
