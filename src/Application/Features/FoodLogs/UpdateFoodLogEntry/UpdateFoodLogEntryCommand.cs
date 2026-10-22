namespace Application;

/// <summary>
/// UC-D03: sửa bữa/giờ/serving/số lượng. KHÔNG đọc lại FoodItem/Recipe hiện tại (xem <see cref="EntryEdit"/>): đổi bữa/giờ giữ nguyên dinh dưỡng; đổi số lượng co giãn từ snapshot đã lưu theo tỉ lệ gram/khẩu phần;
/// đổi khẩu phần dùng gram khẩu phần lúc log (snapshot gốc) và gram khẩu phần mới user chọn. Entry recipe: chỉ đổi số khẩu phần (Quantity, FoodServingId để trống).
/// </summary>
public sealed record UpdateFoodLogEntryCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    MealType? MealType,
    TimeOnly? EatenAt,
    Guid? FoodServingId,
    decimal Quantity
) : IRequest<Unit>;
