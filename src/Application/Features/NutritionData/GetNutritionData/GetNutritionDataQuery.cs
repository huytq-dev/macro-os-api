namespace Application;

/// <summary>
/// Nutrition Data Manager — danh sách các ngày có dữ liệu dinh dưỡng (mới nhất trước) với tổng kcal/protein/carbs/fat của ngày, nguồn (App | Manual) và cờ fasting/incomplete.
/// Đây là NGUỒN DỮ LIỆU DUY NHẤT về lượng ăn của một ngày: expenditure, check-in, dải ngày, dashboard và habits đều đọc cùng FoodLog + entries (qua <see cref="FoodLogSupport.LoadDaysAsync"/>).
/// From/To tuỳ chọn (gồm hai đầu), mặc định mọi ngày.
/// </summary>
public sealed record GetNutritionDataQuery(DateOnly? From, DateOnly? To) : IRequest<NutritionDataResponse>;
