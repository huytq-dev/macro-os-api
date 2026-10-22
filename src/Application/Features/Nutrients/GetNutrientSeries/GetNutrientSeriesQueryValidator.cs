namespace Application;

public sealed class GetNutrientSeriesQueryValidator : AbstractValidator<GetNutrientSeriesQuery>
{
    public GetNutrientSeriesQueryValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage(ValidationMessages.FromAfterTo);
    }
}
