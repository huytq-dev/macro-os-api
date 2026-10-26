namespace Application;

/// <summary>
/// Nutrition Data Manager — danh sách các ngày có dữ liệu dinh dưỡng (mới nhất trước): ngày có entry Food Log, có manual override (Manual Nutrition, S29) hoặc đánh dấu nhịn ăn, mỗi ngày kèm dinh dưỡng HIỆU LỰC
/// (Manual → Food Log → Fasting), phần manual, tổng RAW của Food Log và các cờ. Mỗi ngày giải bằng đúng phép giải của <see cref="FoodLogSupport.LoggedDay"/> (cùng nguồn expenditure, check-in, dải ngày, dashboard, habits).
/// From/To tuỳ chọn (gồm hai đầu). Phân trang: <paramref name="Limit"/> (mặc định 90 = kích thước trang, tối đa 366; KHÔNG phải giới hạn lịch sử) và <paramref name="Before"/> (chỉ ngày TRƯỚC ngày này; dùng NextBefore của trang trước để lấy ngày cũ hơn).
/// </summary>
public sealed record GetNutritionDataQuery(DateOnly? From, DateOnly? To, int? Limit = null, DateOnly? Before = null) : IRequest<NutritionDataResponse>;
