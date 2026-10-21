namespace Contract;

/// <summary>
/// Sản phẩm Open Food Facts khớp tên (CHƯA lưu vào DB): FE chọn dòng nào thì gọi <c>GET /api/foods/barcode/{barcode}</c> để lưu thành món rồi mới log được.
/// Dinh dưỡng /100 g (kcal, g).
/// </summary>
public sealed record OffSearchItem(
    string Barcode,
    string Name,
    string? Brand,
    string? ImageUrl,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    string? ServingName,
    decimal? ServingGrams
);
