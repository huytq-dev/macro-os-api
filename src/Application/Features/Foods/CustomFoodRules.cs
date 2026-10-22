namespace Application;

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
            .WithMessage(ValidationMessages.OneDefaultServing);
        v.RuleFor(nutrients).Must(l => l is null || l.Count() <= 200).WithMessage(ValidationMessages.MaxNutrients200);
        v.RuleFor(nutrients).Must(l => l is null || l.Select(x => x.Code).Distinct().Count() == l.Count()).WithMessage(ValidationMessages.DuplicateNutrientCode);
        v.RuleForEach(nutrients!).SetValidator(new FoodNutrientInputValidator());
    }
}
