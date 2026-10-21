namespace Application;

public sealed class GetNutritionOverviewQueryValidator : AbstractValidator<GetNutritionOverviewQuery>
{
    public GetNutritionOverviewQueryValidator()
    {
        RuleFor(x => x.Range).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
    }
}
