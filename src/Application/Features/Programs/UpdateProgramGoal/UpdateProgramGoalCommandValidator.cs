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
            .InclusiveBetween(-1.5m, 1.5m).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.WeeklyWeightChangeTargetPercent.HasValue);

        RuleFor(x => x.FixedProteinGrams)
            .InclusiveBetween(30, 400).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.FixedProteinGrams.HasValue);

        RuleFor(x => x.CalorieFloorKcal)
            .InclusiveBetween(800, 3000).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.CalorieFloorKcal.HasValue);

        // Xác nhận shift (đề xuất từ Feasibility): cùng luật với Create
        RuleFor(x => x.CalorieShiftMode).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.CalorieShiftMode.HasValue);
        RuleFor(x => x.CalorieShiftPercent).InclusiveBetween(CalorieShiftCalculator.MinPercent, CalorieShiftCalculator.MaxPercent).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.CalorieShiftPercent.HasValue);
        RuleFor(x => x.CalorieShiftPercent).NotNull().WithMessage(ValidationMessages.Required)
            .When(x => x.CalorieShiftMode is { } mode && mode != CalorieShiftMode.Even);   // shift thật cần % (không thì âm thầm không có shift)
        RuleFor(x => x.CalorieShiftDays)
            .Must(days => days is { Count: >= 1 and <= 6 } && days.Distinct().Count() == days.Count)
            .WithMessage(ValidationMessages.RefeedDaysRange)
            .When(x => x.CalorieShiftMode is { } mode && mode != CalorieShiftMode.Even);
    }
}
