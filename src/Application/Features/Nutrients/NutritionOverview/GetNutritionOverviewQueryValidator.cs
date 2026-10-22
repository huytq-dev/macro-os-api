namespace Application;

public sealed class GetNutritionOverviewQueryValidator : AbstractValidator<GetNutritionOverviewQuery>
{
    public GetNutritionOverviewQueryValidator()
    {
        RuleFor(x => x.Range).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Date)
            .NotInFuture()
            .When(x => x.Date.HasValue);
    }
}
