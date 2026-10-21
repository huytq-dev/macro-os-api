namespace Infrastructure;

/// <param name="Candidates">Tối đa 3 ứng viên tốt nhất (cùng thứ tự với bộ chọn) để người duyệt chọn <c>--fdc key=fdcId</c>; rỗng khi không gọi FDC.</param>
public sealed record UsdaVerifyItem(
    string Key, string Name, string Provider, string Status, int? FdcId, string? FdcDescription, string? DataType, decimal Score,
    IReadOnlyList<UsdaDiff> MacroDiffs, IReadOnlyList<UsdaDiff> NutrientDiffs, bool Applied, string? Error = null,
    IReadOnlyList<UsdaVerifyCandidate>? Candidates = null, decimal OurKcal = 0, decimal OurProtein = 0, decimal OurFat = 0, decimal OurCarbs = 0);
