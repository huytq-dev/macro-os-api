using System.Text.Json;

namespace Application;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator()
    {
        RecipeWriter.Rules(this, x => x.Name, x => x.Description, x => x.ImageUrl, x => x.ServingsCount, x => x.PrepTimeMinutes, x => x.CookTimeMinutes,
            x => x.MealType, x => x.SourceUrl, x => x.Instructions, x => x.CookedYieldGrams, x => x.Ingredients);
        RecipePreparation.Rules(this, x => x.Steps, x => x.IconKey);
    }
}
