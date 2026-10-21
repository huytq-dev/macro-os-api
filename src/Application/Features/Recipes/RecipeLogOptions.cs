using System.Text.Json;

namespace Application;

/// <summary>Log recipe vừa tạo: ngày/bữa/giờ như POST /food-logs/entries (thiếu → hôm nay, giờ hiện tại của user), Quantity = số khẩu phần ăn.</summary>
public sealed record RecipeLogOptions(DateOnly? LogDate, MealType? MealType, TimeOnly? EatenAt, decimal Quantity = 1);
