namespace Application;

/// <summary>Một dòng cần log: món (kèm serving hoặc gram) XOR recipe (Quantity = số khẩu phần).</summary>
public sealed record EntryLine(Guid? FoodItemId, Guid? RecipeId, Guid? FoodServingId, decimal Quantity);
