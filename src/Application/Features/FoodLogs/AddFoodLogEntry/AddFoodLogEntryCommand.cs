namespace Application;

/// <summary>UC-D02: thêm entry. Món (FoodItemId): Quantity = số serving (có FoodServingId) hoặc số gram (không có). Recipe (RecipeId): Quantity = số khẩu phần, không có FoodServingId.</summary>
public sealed record AddFoodLogEntryCommand(
    DateOnly? LogDate,
    MealType? MealType,
    TimeOnly? EatenAt,
    Guid? FoodItemId,
    Guid? FoodServingId,
    decimal Quantity,
    Guid? RecipeId = null
) : IRequest<Guid>;
