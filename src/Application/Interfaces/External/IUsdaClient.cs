namespace Application;

/// <param name="Number">Số chất của USDA (vd "203" = Protein), khớp NutrientDefinition.UsdaNumber.</param>
/// <param name="Unit">Đơn vị USDA gốc (g, mg, µg, kcal, IU, kJ).</param>
/// <param name="Amount">Lượng trên 100 g (Foundation / SR Legacy).</param>
public sealed record UsdaNutrientValue(string Number, string Unit, decimal Amount);

/// <param name="Name">Tên khẩu phần đã dựng sẵn, vd "1 cup, sliced".</param>
public sealed record UsdaPortion(string Name, decimal Grams);

public sealed record UsdaFood(
    int FdcId,
    string Description,
    string DataType,
    string? Category,
    IReadOnlyList<UsdaPortion> Portions,
    IReadOnlyList<UsdaNutrientValue> Nutrients
);

public static class UsdaDataTypes
{
    /// <summary>Foundation + SR Legacy: thực phẩm đơn, nutrient chuẩn trên 100 g (nguồn mặc định).</summary>
    public const string Reference = "Foundation,SR Legacy";

    /// <summary>Survey (FNDDS): món chế biến/hỗn hợp, cũng trên 100 g.</summary>
    public const string Survey = "Survey (FNDDS)";
}

/// <summary>USDA FoodData Central (https://fdc.nal.usda.gov). Mặc định lấy Foundation + SR Legacy: nutrient tính sẵn trên 100 g.</summary>
public interface IUsdaClient
{
    /// <param name="dataTypes">Loại dữ liệu FDC, phân cách bằng dấu phẩy ("Foundation,SR Legacy" mặc định; "Survey (FNDDS)" cho món chế biến).</param>
    /// <returns>fdcId theo độ liên quan (tối đa <paramref name="pageSize"/>); lỗi mạng/timeout/hết hạn mức → <see cref="ExternalServiceException"/>.</returns>
    Task<IReadOnlyList<int>> SearchAsync(string query, int pageSize, CancellationToken ct, string dataTypes = UsdaDataTypes.Reference);

    /// <returns>Chi tiết đầy đủ các món (một request); id USDA không có thì vắng mặt trong kết quả.</returns>
    Task<IReadOnlyList<UsdaFood>> GetFoodsAsync(IReadOnlyList<int> fdcIds, CancellationToken ct);
}
