namespace Application;

/// <summary>
/// Tìm món chia nhóm kiểu MacroFactor (From History / Common / Custom / Branded / Open Food Facts). Không có <see cref="Section"/>: cả 5 nhóm, mỗi nhóm top <see cref="OverviewTop"/> + tổng.
/// Có Section: chỉ nhóm đó, phân trang (<see cref="Page"/> từ 1, <see cref="Top"/> mặc định 20) cho nút "See more". Endpoint cũ <c>GET /foods</c> (SearchFoodsQuery) vẫn giữ cho các chỗ khác.
/// </summary>
public sealed record SearchFoodsGroupedQuery(string? Search, FoodSearchSection? Section = null, int Page = 1, int Top = 0) : IRequest<FoodSearchResponse>
{
    public const int OverviewTop = 5, SectionTop = 20, MaxTop = 50;
}
