namespace Contract;

/// <summary>Một nhóm kết quả tìm kiếm: các dòng của trang hiện tại + tổng số món khớp của nhóm (cho nút "See N more").</summary>
public sealed record FoodSearchGroup(IReadOnlyList<FoodItemModel> Items, int TotalCount)
{
    public static readonly FoodSearchGroup Empty = new([], 0);
}
