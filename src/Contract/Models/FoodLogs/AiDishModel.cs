namespace Contract;

public sealed record AiDishModel(Guid Id, string Name, IReadOnlyList<AiIngredientModel> Ingredients);
