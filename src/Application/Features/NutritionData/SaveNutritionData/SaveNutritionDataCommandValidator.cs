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
        RuleFor(x => x).Must(x => x.Calories.HasValue || (x.ProteinG is null && x.CarbsG is null && x.FatG is null))
            .WithName("Calories").WithMessage("Energy is required when protein, carbs or fat are given.");
        RuleFor(x => x).Must(x => !x.IsFasting || ((x.Calories ?? 0) == 0 && (x.ProteinG ?? 0) == 0 && (x.CarbsG ?? 0) == 0 && (x.FatG ?? 0) == 0))
            .WithName("IsFasting").WithMessage("A fasting day has no energy or macros: leave them empty or 0.");
    }
}
