namespace Application;

/// <summary>Tra cứu sản phẩm theo mã vạch và tìm theo tên ở Open Food Facts.</summary>
public interface IOpenFoodFactsClient
{
    /// <returns>null nếu OFF không có sản phẩm hoặc chưa có dữ liệu dinh dưỡng; lỗi mạng/timeout → <see cref="ExternalServiceException"/>.</returns>
    Task<OffProduct?> FindByBarcodeAsync(string code, CancellationToken ct);

    /// <summary>Tìm theo tên (trang bắt đầu từ 1). Timeout ngắn; lỗi mạng/timeout → <see cref="ExternalServiceException"/> (caller coi nhóm OFF là không khả dụng).</summary>
    Task<OffSearchResult> SearchByNameAsync(string query, int page, int pageSize, CancellationToken ct);
}
