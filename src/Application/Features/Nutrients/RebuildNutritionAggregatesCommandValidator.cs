namespace Application;

public sealed class RebuildNutritionAggregatesCommandValidator : AbstractValidator<RebuildNutritionAggregatesCommand>
{
    public RebuildNutritionAggregatesCommandValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || (x.To >= x.From && x.To.Value.DayNumber - x.From.Value.DayNumber <= 730))
            .WithName("From").WithMessage(ValidationMessages.InvalidDateRange730);
}
