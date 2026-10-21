namespace Application;

public sealed class UpdateFoodLogSettingsCommandValidator : AbstractValidator<UpdateFoodLogSettingsCommand>
{
    public UpdateFoodLogSettingsCommandValidator()
    {
        RuleFor(x => x.Overages).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Alignment).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.AddFoodsToHour).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.WeekBanner).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.CalendarScrolling).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Optimization).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.TimelineTileDensity).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.TimelineHighlight).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.LoggerBannerDisplay).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.LoggerTileDensity).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.LoggerHighlight).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);

        RuleFor(x => x.HourRangeFirst).InclusiveBetween(0, 23).WithMessage(ValidationMessages.Range);
        RuleFor(x => x.HourRangeLast).InclusiveBetween(0, 23).WithMessage(ValidationMessages.Range);
        RuleFor(x => x).Must(x => x.HourRangeLast - x.HourRangeFirst >= 1)
            .WithMessage("HourRangeLast phải lớn hơn HourRangeFirst ít nhất 1 giờ.")
            .When(x => x.HourRangeFirst is >= 0 and <= 23 && x.HourRangeLast is >= 0 and <= 23);
    }
}
