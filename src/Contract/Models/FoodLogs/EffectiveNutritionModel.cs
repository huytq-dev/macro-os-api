namespace Contract;

/// <summary>
/// Dinh dưỡng HIỆU LỰC của một ngày (Contract v2), do BE quyết định theo thứ tự Manual → Food Log → Fasting (FE KHÔNG tự cộng/trừ).
/// Hai nhóm số tách bạch: <b>Calories/ProteinG/CarbsG/FatG</b> = effective; <b>Manual*</b> = đúng những gì user nhập (null = chưa nhập, 0 = nhập 0); <b>FoodLog*</b> = tổng RAW các món Food Log (không bao giờ bị manual đổi).
/// </summary>
/// <param name="Calories">kcal hiệu lực; null = chưa biết (override không nhập calories; không suy từ macro, không lấy từ Food Log). Hiển thị "—", không phải 0.</param>
/// <param name="Source">Manual (có override) | FoodLog | Fasting | Imported (chưa dùng). Ngày không có dữ liệu không xuất hiện (Missing/Unlogged).</param>
/// <param name="HasOverride">Ngày có ít nhất một trường manual khác null.</param>
/// <param name="IsValidForAlgorithms">Ngày có tham gia thuật toán Expenditure/Energy Balance/check-in hay không (tách khỏi Calories): false khi chưa đủ, kcal chưa biết, hoặc 0 kcal không Fasting. Fasting 0 kcal = true.</param>
/// <param name="Conflict">Xung đột chỉ để báo cáo (BE không tự xử lý): MANUAL_ZERO_WITH_FOOD_LOG | FASTING_WITH_MANUAL_NUTRITION; null nếu không có.</param>
public sealed record EffectiveNutritionModel(
    decimal? Calories,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG,
    string Source,
    bool HasOverride,
    decimal? ManualCaloriesKcal,
    decimal? ManualProteinG,
    decimal? ManualCarbsG,
    decimal? ManualFatG,
    decimal FoodLogCalories,
    decimal FoodLogProteinG,
    decimal FoodLogCarbsG,
    decimal FoodLogFatG,
    bool IsValidForAlgorithms,
    string? Conflict
);
