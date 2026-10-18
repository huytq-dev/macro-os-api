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

/// <summary>USDA FoodData Central (https://fdc.nal.usda.gov). Chỉ lấy Foundation + SR Legacy: nutrient tính sẵn trên 100 g.</summary>
public interface IUsdaClient
{
    /// <returns>fdcId theo độ liên quan (tối đa <paramref name="pageSize"/>); lỗi mạng/timeout/hết hạn mức → <see cref="ExternalServiceException"/>.</returns>
    Task<IReadOnlyList<int>> SearchAsync(string query, int pageSize, CancellationToken ct);

    /// <returns>Chi tiết đầy đủ các món (một request); id USDA không có thì vắng mặt trong kết quả.</returns>
    Task<IReadOnlyList<UsdaFood>> GetFoodsAsync(IReadOnlyList<int> fdcIds, CancellationToken ct);
}
