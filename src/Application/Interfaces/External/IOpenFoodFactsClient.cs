namespace Application;

/// <param name="Nutrition">Dinh dưỡng /100 g (đã đổi g → mg/µg theo <see cref="FoodNutrition"/>).</param>
/// <param name="ServingGrams">Khẩu phần theo nhãn (gram); null nếu nhãn không ghi.</param>
public sealed record OffProduct(
    string Code,
    string Name,
    string? Brand,
    string? ImageUrl,
    decimal? ServingGrams,
    string? ServingName,
    FoodNutrition Nutrition
);

/// <param name="Items">Sản phẩm có dinh dưỡng của trang hiện tại (đã bỏ sản phẩm thiếu mã/dinh dưỡng).</param>
/// <param name="TotalCount">Tổng số sản phẩm OFF khớp từ khoá (kể cả sản phẩm thiếu dinh dưỡng).</param>
public sealed record OffSearchResult(IReadOnlyList<OffProduct> Items, int TotalCount);

/// <summary>Tra cứu sản phẩm theo mã vạch và tìm theo tên ở Open Food Facts.</summary>
public interface IOpenFoodFactsClient
{
    /// <returns>null nếu OFF không có sản phẩm hoặc chưa có dữ liệu dinh dưỡng; lỗi mạng/timeout → <see cref="ExternalServiceException"/>.</returns>
    Task<OffProduct?> FindByBarcodeAsync(string code, CancellationToken ct);

    /// <summary>Tìm theo tên (trang bắt đầu từ 1). Timeout ngắn; lỗi mạng/timeout → <see cref="ExternalServiceException"/> (caller coi nhóm OFF là không khả dụng).</summary>
    Task<OffSearchResult> SearchByNameAsync(string query, int page, int pageSize, CancellationToken ct);
}
