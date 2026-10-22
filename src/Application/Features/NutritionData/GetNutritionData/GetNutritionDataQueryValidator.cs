namespace Application;

public sealed class GetNutritionDataQueryValidator : AbstractValidator<GetNutritionDataQuery>
{
    public GetNutritionDataQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage(ValidationMessages.FromAfterTo);
}
