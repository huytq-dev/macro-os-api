namespace Application;

public sealed record AiDraftDish(string Name, IReadOnlyList<AiDraftIngredient> Ingredients);
