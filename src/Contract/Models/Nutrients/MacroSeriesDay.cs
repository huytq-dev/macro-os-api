namespace Contract;

/// <summary>
/// Một ngày của biểu đồ Macros: kcal xếp chồng carbs ×4 + fat ×9 + protein ×4 + Other. Giá trị lấy từ tổng FoodLog của ngày (cùng nguồn <c>GET /nutrients/{code}/series</c>), ngày theo múi giờ user.
/// Ngày CHƯA log (không entry, không nhịn ăn): <c>IsLogged = false</c> và mọi giá trị null (không bịa 0). Ngày nhịn ăn: logged, mọi giá trị 0.
/// </summary>
/// <param name="IsIncomplete">User đánh dấu log chưa đủ: vẫn có số nhưng KHÔNG vào thống kê/bucket (như series từng nutrient).</param>
/// <param name="CaloriesKcal">Tổng kcal của ngày (cột kcal của entry, KHÔNG tính lại từ macro).</param>
/// <param name="ProteinKcal">ProteinG × 4.</param>
/// <param name="FatKcal">FatG × 9.</param>
/// <param name="CarbsKcal">CarbsG × 4.</param>
/// <param name="OtherKcal">CaloriesKcal − (ProteinKcal + FatKcal + CarbsKcal): năng lượng không quy được về P/F/C (quick-add chỉ nhập kcal, cồn 7 kcal/g, sai khác dữ liệu nguồn). Giá trị THÔ CÓ DẤU: âm khi kcal của entry nhỏ hơn tổng macro (không kẹp, không bịa); FE quyết định cách vẽ.</param>
public sealed record MacroSeriesDay(
    DateOnly Date,
    bool IsLogged,
    bool IsFasting,
    bool IsIncomplete,
    decimal? CaloriesKcal,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    decimal? ProteinKcal,
    decimal? FatKcal,
    decimal? CarbsKcal,
    decimal? OtherKcal
);
