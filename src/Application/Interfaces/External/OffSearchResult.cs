namespace Application;

/// <param name="Items">Sản phẩm có dinh dưỡng của trang hiện tại (đã bỏ sản phẩm thiếu mã/dinh dưỡng).</param>
/// <param name="TotalCount">Tổng số sản phẩm OFF khớp từ khoá (kể cả sản phẩm thiếu dinh dưỡng).</param>
public sealed record OffSearchResult(IReadOnlyList<OffProduct> Items, int TotalCount);
