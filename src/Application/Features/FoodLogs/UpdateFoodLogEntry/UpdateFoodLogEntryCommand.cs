namespace Application;

/// <summary>
/// UC-D03: sửa bữa/giờ/serving/số lượng. Entry món: snapshot tính lại từ FoodItem hiện tại.
/// Entry recipe: chỉ đổi số khẩu phần (Quantity, FoodServingId để trống); snapshot cũ nhân theo tỉ lệ, không đọc lại recipe.
/// </summary>
public sealed record UpdateFoodLogEntryCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    MealType? MealType,
    TimeOnly? EatenAt,
    Guid? FoodServingId,
    decimal Quantity
) : IRequest<Unit>;
