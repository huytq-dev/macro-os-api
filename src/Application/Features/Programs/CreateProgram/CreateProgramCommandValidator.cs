namespace Application;

public sealed class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.GoalType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.ProgramType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.DietStyle).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.ProteinLevel).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);

        // MacroFactor khuyến nghị 0.25–1 %/tuần; cho phép tới 1.5 nhưng không hơn
        RuleFor(x => x.WeeklyWeightChangeTargetPercent)
            .InclusiveBetween(-1.5m, 1.5m).WithMessage(ValidationMessages.Range);

        RuleFor(x => x.WeeklyWeightChangeTargetPercent)
            .Equal(0).WithMessage("{PropertyName} phải bằng 0 khi mục tiêu là Maintenance")
            .When(x => x.GoalType == GoalType.Maintenance);

        RuleFor(x => x.CalorieFloorKcal)
            .InclusiveBetween(800, 3000).WithMessage(ValidationMessages.Range)
            .When(x => x.CalorieFloorKcal.HasValue);

        RuleFor(x => x.ManualInitialTdeeKcal)
            .InclusiveBetween(1000, 6000).WithMessage(ValidationMessages.Range)
            .When(x => x.ManualInitialTdeeKcal.HasValue);

        RuleFor(x => x.CheckInWeekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.CheckInWeekday.HasValue);
    }
}
