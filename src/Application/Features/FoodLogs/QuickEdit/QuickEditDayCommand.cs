namespace Application;

/// <summary>
/// WP6 / UC-D14: Quick Edit ngày — thay toàn bộ entry của ngày bằng đúng một quick add (kcal/P/C/F nhập tay). Entry cũ bị xoá mềm.
/// MealType/EatenAt thiếu → suy như quick add thường.
/// </summary>
public sealed record QuickEditDayCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] DateOnly Date,
    string? Name,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    MealType? MealType,
    TimeOnly? EatenAt
) : IRequest<Guid>;
