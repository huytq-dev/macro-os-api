namespace Application;

/// <summary>Một món/recipe trong plate. MealType/EatenAt của dòng thiếu → lấy của lô; lô cũng thiếu → suy như thêm đơn lẻ.</summary>
public sealed record BatchEntryItem(
    Guid? FoodItemId,
    Guid? RecipeId,
    Guid? FoodServingId,
    decimal Quantity,
    MealType? MealType = null,
    TimeOnly? EatenAt = null);
