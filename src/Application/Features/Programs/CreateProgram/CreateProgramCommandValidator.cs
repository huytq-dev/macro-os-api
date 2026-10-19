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
            .Equal(0).WithMessage(ValidationMessages.MustBeZeroForMaintenance)
            .When(x => x.GoalType == GoalType.Maintenance);

        RuleFor(x => x.CalorieFloorKcal)
            .InclusiveBetween(800, 3000).WithMessage(ValidationMessages.Range)
            .When(x => x.CalorieFloorKcal.HasValue);

        RuleFor(x => x.ManualInitialTdeeKcal)
            .InclusiveBetween(1000, 6000).WithMessage(ValidationMessages.Range)
            .When(x => x.ManualInitialTdeeKcal.HasValue);

        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range)
            .When(x => x.TargetWeightKg.HasValue);
        RuleFor(x => x.TrainingType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.TrainingType.HasValue);
        RuleFor(x => x.CalorieShiftMode).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.CalorieShiftMode.HasValue);
        RuleFor(x => x.CalorieShiftPercent).InclusiveBetween(5m, 30m).WithMessage(ValidationMessages.Range)
            .When(x => x.CalorieShiftPercent.HasValue);
        RuleFor(x => x.CalorieShiftDays)
            .Must(days => days is { Count: >= 1 and <= 6 } && days.Distinct().Count() == days.Count)
            .WithMessage(ValidationMessages.RefeedDaysRange)
            .When(x => x.CalorieShiftMode is { } mode && mode != CalorieShiftMode.Even);

        RuleFor(x => x.CheckInWeekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.CheckInWeekday.HasValue);

        // Target theo ngày: Manual bắt buộc đủ 7 ngày, Coached không có, Collaborative tuỳ chọn. Trần phụ thuộc TDEE/cân nặng được kiểm ở handler
        RuleFor(x => x.DayTargets)
            .Must(d => d is { Count: 7 }).WithMessage(ValidationMessages.ManualProgramNeedsSevenDayTargets)
            .When(x => x.ProgramType == ProgramType.Manual);
        RuleFor(x => x.DayTargets)
            .Must(d => d is null or { Count: 0 }).WithMessage(ValidationMessages.CoachedProgramNoDayTargets)
            .When(x => x.ProgramType == ProgramType.Coached);
        RuleFor(x => x.DayTargets)
            .Must(d => d is null or { Count: 0 } or { Count: 7 }).WithMessage(ValidationMessages.DayTargetsNeedSevenDays)
            .When(x => x.ProgramType == ProgramType.Collaborative);
        RuleFor(x => x.DayTargets)
            .Must(d => d!.Select(t => t.DayOfWeek).Distinct().Count() == d!.Count).WithMessage(ValidationMessages.DayTargetsDistinctWeekdays)
            .When(x => x.DayTargets is { Count: > 0 });
        RuleForEach(x => x.DayTargets).ChildRules(day =>
        {
            day.RuleFor(t => t.DayOfWeek).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
            day.RuleFor(t => t.Calories).InclusiveBetween(0, 10000).WithMessage(ValidationMessages.Range);
            day.RuleFor(t => t.ProteinG).InclusiveBetween(0m, 1000m).WithMessage(ValidationMessages.Range);
            day.RuleFor(t => t.CarbsG).InclusiveBetween(0m, 2000m).WithMessage(ValidationMessages.Range);
            day.RuleFor(t => t.FatG).InclusiveBetween(0m, 1000m).WithMessage(ValidationMessages.Range);
        }).When(x => x.DayTargets is { Count: > 0 });
    }
}
