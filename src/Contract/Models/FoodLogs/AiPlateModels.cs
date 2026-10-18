namespace Contract;

/// <summary>Nguyên liệu AI nhận diện: chỉ tên (tiếng Anh ngắn) và gram ước lượng, chưa có dinh dưỡng; FE gọi resolve-ingredient để tìm trong DB.</summary>
public sealed record AiIngredientModel(Guid Id, string Name, decimal Grams);

public sealed record AiDishModel(Guid Id, string Name, IReadOnlyList<AiIngredientModel> Ingredients);

/// <param name="Estimated">Luôn true: món/nguyên liệu/gram do AI nhận diện, FE hiện dòng cảnh báo.</param>
/// <param name="Confidence">low | medium | high, độ tin cậy chung do model tự đánh giá.</param>
/// <param name="ScanId">Id bản ghi phân tích đã lưu (kết quả, không lưu ảnh); gửi lại ở POST /food-logs/ai/confirm khi user log để hệ thống nhớ lựa chọn nguyên liệu.</param>
public sealed record AiAnalysisResponse(IReadOnlyList<AiDishModel> Dishes, bool Estimated, string Confidence, Guid? ScanId = null);

/// <param name="Status">Confirmed sau khi xác nhận.</param>
/// <param name="Remembered">Số nguyên liệu đã ghi vào bộ nhớ đệm.</param>
public sealed record AiScanConfirmResponse(Guid ScanId, string Status, int Remembered);

/// <summary>
/// Kết quả tìm một nguyên liệu trong DB thực phẩm. Macro là TỔNG cho đúng <paramref name="WeightGrams"/> (= Quantity × gram của khẩu phần), không phải /100 g.
/// Matched=true: có <paramref name="FoodItemId"/> thật, log qua POST /food-logs/entries (FoodItemId + ServingId + Quantity).
/// Matched=false &amp;&amp; Estimated=true: không có trong DB, số do AI ước lượng, log bằng quick-add.
/// Matched=false &amp;&amp; Estimated=false: không có gì, macro = 0 (không bịa số), user tự nhập.
/// </summary>
public sealed record AiResolvedIngredientModel(
    bool Matched,
    Guid? FoodItemId,
    string? FoodName,
    string? Emoji,
    Guid? ServingId,
    decimal Quantity,
    string UnitLabel,
    decimal WeightGrams,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    bool Estimated,
    string? IconKey = null
);
