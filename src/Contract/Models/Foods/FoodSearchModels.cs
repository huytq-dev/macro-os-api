namespace Contract;

/// <summary>Một nhóm kết quả tìm kiếm: các dòng của trang hiện tại + tổng số món khớp của nhóm (cho nút "See N more").</summary>
public sealed record FoodSearchGroup(IReadOnlyList<FoodItemModel> Items, int TotalCount)
{
    public static readonly FoodSearchGroup Empty = new([], 0);
}

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

/// <param name="Available">false = Open Food Facts lỗi/timeout/bị tắt trong cài đặt user: nhóm rỗng, FE ẩn nhóm (không coi là "không có kết quả").</param>
public sealed record OffSearchGroup(IReadOnlyList<OffSearchItem> Items, int TotalCount, bool Available)
{
    public static readonly OffSearchGroup Unavailable = new([], 0, false);
}

/// <summary>
/// Kết quả tìm kiếm chia nhóm kiểu MacroFactor. Gọi không có <c>section</c>: cả 5 nhóm, mỗi nhóm top 5 + tổng.
/// Gọi có <c>section</c> (+ <c>page</c>, <c>top</c>): chỉ nhóm đó có dữ liệu (phân trang cho "See more"), các nhóm khác rỗng.
/// </summary>
/// <param name="History">Món user đã log và khớp từ khoá (gần nhất trước); không có từ khoá = các món log gần đây.</param>
/// <param name="Common">Món hệ thống (catalog + USDA Foundation/SR/FNDDS, không thương hiệu); trống khi chưa gõ từ khoá.</param>
/// <param name="Custom">Món user tự tạo.</param>
/// <param name="Branded">Món có thương hiệu đã có trong DB (vd sản phẩm Open Food Facts từng quét mã vạch); ẩn khi user tắt "Branded".</param>
/// <param name="OpenFoodFacts">Tìm theo tên trực tiếp ở Open Food Facts; ẩn khi user tắt "Open Food Facts".</param>
public sealed record FoodSearchResponse(
    string Query,
    FoodSearchGroup History,
    FoodSearchGroup Common,
    FoodSearchGroup Custom,
    FoodSearchGroup Branded,
    OffSearchGroup OpenFoodFacts
);
