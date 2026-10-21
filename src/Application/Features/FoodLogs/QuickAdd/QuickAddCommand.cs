namespace Application;

/// <summary>UC-D04: nhập thẳng kcal/P/C/F. Tạo FoodItem ẩn Source=QuickAdd (giá trị = /100 g) và entry 100 g.</summary>
public sealed record QuickAddCommand(
    DateOnly? LogDate,
    MealType? MealType,
    TimeOnly? EatenAt,
    string? Name,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat
) : IRequest<Guid>;
