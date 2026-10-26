namespace Application;

public sealed class SaveNutritionDataCommandValidator : AbstractValidator<SaveNutritionDataCommand>
{
    public SaveNutritionDataCommandValidator()
    {
        RuleFor(x => x.Date).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000).When(x => x.Calories.HasValue);
        RuleFor(x => x.ProteinG).InclusiveBetween(0, 1000).When(x => x.ProteinG.HasValue);
        RuleFor(x => x.CarbsG).InclusiveBetween(0, 1000).When(x => x.CarbsG.HasValue);
        RuleFor(x => x.FatG).InclusiveBetween(0, 1000).When(x => x.FatG.HasValue);
        // Macro-only được phép (calories null = chưa biết, không phải 0); nhịn ăn trong cùng request không được kèm giá trị dương
        RuleFor(x => x).Must(x => x.IsFasting != true || !NutritionDataSupport.ManualPositive(x.Calories, x.ProteinG, x.CarbsG, x.FatG))
            .WithName("IsFasting").WithMessage(ValidationMessages.FastingDayNoEnergy)
            .WithErrorCode(NutritionDataSupport.ReasonFastingWithManual);   // GlobalExceptionHandler đưa mã dạng HOA_GẠCH_DƯỚI này vào errors.reasonCode (cùng mã với đường handler)
    }
}
