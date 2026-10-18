namespace Contract;

/// <summary>
/// Một preset yêu thích: món + khẩu phần + số lượng đã chọn (cùng một món có thể có nhiều preset).
/// Dinh dưỡng đã tính sẵn cho đúng lượng preset.
/// </summary>
/// <param name="Quantity">Số khẩu phần nếu có FoodServingId, ngược lại là số gram (giống FoodLogEntry).</param>
/// <param name="Grams">Tổng gram của preset.</param>
public sealed record FavoriteFoodModel(
    Guid Id,
    Guid FoodItemId,
    string FoodName,
    string? Brand,
    string Category,
    Guid? FoodServingId,
    string ServingName,
    decimal Quantity,
    decimal Grams,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    int SortOrder,
    string? IconKey = null
);
