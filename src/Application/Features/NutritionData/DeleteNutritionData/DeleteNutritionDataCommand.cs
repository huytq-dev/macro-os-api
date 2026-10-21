namespace Application;

/// <summary>
/// Xoá một ngày từ NDM. Ngày nhập tay / nhịn ăn / chỉ có cờ: xoá cả ngày như DELETE /food-logs/{date}. Ngày Source = App (có món log từ Food Log): 400, không xoá gì — xoá món ở Food Log.
/// 404 nếu user không có ngày đó.
/// </summary>
public sealed record DeleteNutritionDataCommand(DateOnly Date) : IRequest<Unit>;
