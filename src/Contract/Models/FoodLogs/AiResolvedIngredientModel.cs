namespace Contract;

/// <summary>
/// Kết quả tìm một nguyên liệu trong DB thực phẩm. Macro là TỔNG cho đúng <paramref name="WeightGrams"/> (= Quantity × gram của khẩu phần), không phải /100 g.
/// Matched=true: có <paramref name="FoodItemId"/> thật, log qua POST /food-logs/entries (FoodItemId + ServingId + Quantity).
/// Matched=false &amp;&amp; Estimated=true: không có trong DB, số do AI ước lượng, log bằng quick-add.
/// Matched=false &amp;&amp; Estimated=false: không có gì, macro = 0 (không bịa số), user tự nhập.
/// <paramref name="UnitLabel"/> là tên đơn vị để ghép sau Quantity ("large" → "3 large"), đã bỏ tiền tố "1 " của tên khẩu phần; "g" khi log theo gram.
/// <paramref name="Servings"/> các khẩu phần của món khớp (mặc định đứng đầu, rồi theo gram tăng dần); null khi không khớp DB. Chỉ khớp DB mới có.
/// <paramref name="Source"/> nguồn khớp: cache (user đã xác nhận trước đó) | canonical (dòng đại diện của thực phẩm chuẩn) | variant (biến thể khác theo tên hoặc state) | search (tìm rộng) | ai (AI ước lượng) | none.
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
    string? IconKey = null,
    string? Source = null,
    IReadOnlyList<AiServingModel>? Servings = null
);
