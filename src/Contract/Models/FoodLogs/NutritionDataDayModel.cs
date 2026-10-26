namespace Contract;

/// <summary>
/// Một ngày trong Nutrition Data Manager (Contract v2): dinh dưỡng HIỆU LỰC của ngày (Manual → Food Log → Fasting; nguồn chung của expenditure, check-in, dashboard, dải ngày), phần manual người dùng nhập,
/// tổng RAW của Food Log, và các cờ. FE hiển thị đúng các giá trị này, không tự tính lại.
/// </summary>
/// <param name="Calories">kcal hiệu lực; null = chưa biết (override macro-only). FE giữ null, hiển thị "—", không chuyển thành 0.</param>
/// <param name="ProteinG">Protein hiệu lực; null = chưa nhập (manual) — không lấy từ Food Log, không phải 0.</param>
/// <param name="Source">Manual = có override | FoodLog = các món Food Log (gồm Quick Add và Quick Add cũ) | Fasting = ngày nhịn ăn đánh dấu tay | Imported (chưa dùng). Thay thế App/Manual của v1 (App → FoodLog).</param>
/// <param name="IsFasting">Ngày nhịn ăn: tính là ngày log 0 kcal, không phá chuỗi log.</param>
/// <param name="IsIncomplete">Ngày log chưa đủ: expenditure và trung bình check-in bỏ qua ngày này (tổng ngày vẫn hiện).</param>
/// <param name="EntryCount">Số entry Food Log chưa xoá của ngày (không tính override).</param>
/// <param name="HasOverride">Ngày có manual override (ít nhất một trường manual khác null).</param>
/// <param name="ManualCaloriesKcal">Giá trị manual user nhập (null = chưa nhập; 0 = nhập 0). Cũng là dữ liệu để prefill Edit Day.</param>
/// <param name="FoodLogCalories">Tổng RAW của các món Food Log (không đổi khi có override).</param>
/// <param name="IsValidForAlgorithms">Ngày có tham gia thuật toán hay không (tách khỏi Calories): manual 0 không Fasting = false; Fasting 0 = true.</param>
/// <param name="Conflict">Xung đột chỉ báo cáo (MANUAL_ZERO_WITH_FOOD_LOG | FASTING_WITH_MANUAL_NUTRITION), BE không tự xử lý.</param>
public sealed record NutritionDataDayModel(
    DateOnly Date,
    decimal? Calories,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    string Source,
    bool IsFasting,
    bool IsIncomplete,
    int EntryCount,
    bool HasOverride,
    decimal? ManualCaloriesKcal,
    decimal? ManualProteinG,
    decimal? ManualFatG,
    decimal? ManualCarbsG,
    decimal FoodLogCalories,
    decimal FoodLogProteinG,
    decimal FoodLogFatG,
    decimal FoodLogCarbsG,
    bool IsValidForAlgorithms,
    string? Conflict
);
