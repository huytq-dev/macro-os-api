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

public sealed class FoodServingInputValidator : AbstractValidator<FoodServingInput>
{
    public FoodServingInputValidator()
    {
        RuleFor(x => x.ServingName).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.WeightInGrams).InclusiveBetween(0.1m, 10000);
    }
}

public sealed class FoodNutrientInputValidator : AbstractValidator<FoodNutrientInput>
{
    public FoodNutrientInputValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(50).WithMessage(ValidationMessages.MaxLength)
            .Must(c => !NutrientSnapshot.FoodColumnCodes.Contains(c)).WithMessage("4 macro (kcal, protein, carbs, fat) nhập ở Nutrition, không nhập ở Nutrients.");
        RuleFor(x => x.AmountPer100g).InclusiveBetween(0, 1_000_000m);
    }
}

/// <summary>Rule chung Create/Update custom food.</summary>
public static class CustomFoodRules
{
    public static void Apply<T>(AbstractValidator<T> v,
        Expression<Func<T, string>> name, Expression<Func<T, string?>> brand, Expression<Func<T, string?>> barcode,
        Expression<Func<T, FoodCategory>> category, Expression<Func<T, FoodNutrition>> nutrition,
        Expression<Func<T, IEnumerable<FoodServingInput>?>> servings, Expression<Func<T, IEnumerable<FoodNutrientInput>?>> nutrients,
        Expression<Func<T, string?>>? iconKey = null)
    {
        if (iconKey is not null)
            v.RuleFor(iconKey).Must(FoodIconKeys.IsValid).When(k => iconKey.Compile()(k) is not null).WithMessage(ValidationMessages.InvalidIconKey);
        v.RuleFor(name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(brand).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(barcode).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(category).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        v.RuleFor(nutrition).NotNull().WithMessage(ValidationMessages.Required).SetValidator(new FoodNutritionValidator());
        v.RuleForEach(servings!).SetValidator(new FoodServingInputValidator());
        v.RuleFor(servings).Must(s => s is null || s.Count(x => x.IsDefault) <= 1)
            .WithMessage("Chỉ được một serving mặc định.");
        v.RuleFor(nutrients).Must(l => l is null || l.Count() <= 200).WithMessage("Tối đa 200 chất.");
        v.RuleFor(nutrients).Must(l => l is null || l.Select(x => x.Code).Distinct().Count() == l.Count()).WithMessage("Mã chất bị trùng.");
        v.RuleForEach(nutrients!).SetValidator(new FoodNutrientInputValidator());
    }
}
