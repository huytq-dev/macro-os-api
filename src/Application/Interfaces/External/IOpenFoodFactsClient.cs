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

/// <summary>Tra cứu sản phẩm theo mã vạch ở Open Food Facts.</summary>
public interface IOpenFoodFactsClient
{
    /// <returns>null nếu OFF không có sản phẩm hoặc chưa có dữ liệu dinh dưỡng; lỗi mạng/timeout → <see cref="ExternalServiceException"/>.</returns>
    Task<OffProduct?> FindByBarcodeAsync(string code, CancellationToken ct);
}
