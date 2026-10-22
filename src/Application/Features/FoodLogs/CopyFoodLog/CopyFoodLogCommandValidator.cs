namespace Application;

public sealed class CopyFoodLogCommandValidator : AbstractValidator<CopyFoodLogCommand>
{
    public CopyFoodLogCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.MealType.HasValue);
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
        RuleFor(x => x).Must(x => x.FromDate != x.ToDate || (x.MealType.HasValue && x.ToMealType.HasValue && x.MealType != x.ToMealType))
            .WithMessage(ValidationMessages.SourceEqualsTarget);
    }
}
