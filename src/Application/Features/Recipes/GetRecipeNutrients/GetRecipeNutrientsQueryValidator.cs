using System.Text.Json;

namespace Application;

public sealed class GetRecipeNutrientsQueryValidator : AbstractValidator<GetRecipeNutrientsQuery>
{
    public GetRecipeNutrientsQueryValidator()
    {
        RuleFor(x => x.ServingsCount).InclusiveBetween(1, 100);
        RuleFor(x => x.Ingredients).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count <= 100).WithMessage("Tối đa 100 nguyên liệu.");
        RuleForEach(x => x.Ingredients).ChildRules(i =>
        {
            i.RuleFor(x => x.FoodItemId).NotEmpty().WithMessage(ValidationMessages.Required);
            i.RuleFor(x => x.FoodServingId).Must(id => id is null || id != Guid.Empty).WithMessage(ValidationMessages.Required);
            i.RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
        });
    }
}
