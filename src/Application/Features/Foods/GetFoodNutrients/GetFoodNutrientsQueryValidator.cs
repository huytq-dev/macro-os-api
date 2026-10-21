namespace Application;

public sealed class GetFoodNutrientsQueryValidator : AbstractValidator<GetFoodNutrientsQuery>
{
    public GetFoodNutrientsQueryValidator() => RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
}
