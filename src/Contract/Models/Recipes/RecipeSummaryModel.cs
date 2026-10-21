namespace Contract;

/// <param name="PerServing">Tổng cả recipe chia số khẩu phần.</param>
public sealed record RecipeSummaryModel(
    Guid Id,
    string Name,
    string? ImageUrl,
    int ServingsCount,
    string MealType,
    RecipeMacros PerServing,
    DateTimeOffset UpdatedAt,
    string? IconKey = null
);
