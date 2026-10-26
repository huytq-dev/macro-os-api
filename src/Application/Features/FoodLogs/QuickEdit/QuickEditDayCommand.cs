namespace Application;

/// <summary>
/// Quick Edit / Edit Day: đặt Manual Nutrition của ngày (kcal/P/C/F, S29) ở 4 cột riêng của FoodLogs. Không tạo/sửa/xoá món (FoodLogEntry), ghi chú, nước; override thắng Food Log, không cộng với Food Log.
/// Trả Id của dòng ngày (FoodLog), không còn là Id entry. Name/MealType/EatenAt được giữ trong hợp đồng để tương thích nhưng bị bỏ qua (tổng ngày không phải món).
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
