namespace Application;

public sealed class FoodNutritionValidator : AbstractValidator<FoodNutrition>
{
    public FoodNutritionValidator()
    {
        RuleFor(x => x.CaloriesPer100g).InclusiveBetween(0, 900);
        RuleFor(x => x.ProteinPer100g).InclusiveBetween(0, 100);
        RuleFor(x => x.CarbsPer100g).InclusiveBetween(0, 100);
        RuleFor(x => x.FatPer100g).InclusiveBetween(0, 100);
        RuleFor(x => x.ProteinPer100g + x.CarbsPer100g + x.FatPer100g)
            .LessThanOrEqualTo(105).WithName("Macros").WithMessage(ValidationMessages.LessThanOrEqual);

        foreach (var rule in new Expression<Func<FoodNutrition, decimal?>>[]
        {
            x => x.FiberPer100g, x => x.SugarPer100g, x => x.SaturatedFatPer100g, x => x.TransFatPer100g,
            x => x.SodiumPer100g, x => x.PotassiumPer100g, x => x.CalciumPer100g, x => x.IronPer100g,
            x => x.VitaminCPer100g, x => x.VitaminDPer100g, x => x.CholesterolPer100g,
        })
            RuleFor(rule).GreaterThanOrEqualTo(0).WithMessage(ValidationMessages.GreaterThanOrEqual);
    }
}
