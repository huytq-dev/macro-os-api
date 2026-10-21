namespace Contract;

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
