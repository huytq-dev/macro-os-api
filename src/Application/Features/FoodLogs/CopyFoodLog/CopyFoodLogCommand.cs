namespace Application;

/// <summary>UC-D05: copy bữa (MealType) hoặc cả ngày (MealType null) sang ngày khác; snapshot copy nguyên, không đọc lại FoodItem.</summary>
public sealed record CopyFoodLogCommand(
    DateOnly FromDate,
    DateOnly ToDate,
    MealType? MealType,
    MealType? ToMealType
) : IRequest<int>;
