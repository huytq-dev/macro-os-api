namespace Application;

/// <summary>USDA FoodData Central (https://fdc.nal.usda.gov). Mặc định lấy Foundation + SR Legacy: nutrient tính sẵn trên 100 g.</summary>
public interface IUsdaClient
{
    /// <param name="dataTypes">Loại dữ liệu FDC, phân cách bằng dấu phẩy ("Foundation,SR Legacy" mặc định; "Survey (FNDDS)" cho món chế biến).</param>
    /// <returns>fdcId theo độ liên quan (tối đa <paramref name="pageSize"/>); lỗi mạng/timeout/hết hạn mức → <see cref="ExternalServiceException"/>.</returns>
    Task<IReadOnlyList<int>> SearchAsync(string query, int pageSize, CancellationToken ct, string dataTypes = UsdaDataTypes.Reference);

    /// <returns>Chi tiết đầy đủ các món (một request); id USDA không có thì vắng mặt trong kết quả.</returns>
    Task<IReadOnlyList<UsdaFood>> GetFoodsAsync(IReadOnlyList<int> fdcIds, CancellationToken ct);
}
