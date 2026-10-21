namespace Application;

/// <summary>UC-C01: tìm theo tên/brand; All = hệ thống + của tôi (không gồm custom của người khác, không gồm QuickAdd).</summary>
public sealed record SearchFoodsQuery(
    string? Search,
    FoodCategory? Category,
    FoodScope Scope = FoodScope.All,
    int Top = AppConstants.DefaultPageTop,
    int Skip = AppConstants.DefaultPageSkip
) : IRequest<QueryResult<FoodItemModel>>;
