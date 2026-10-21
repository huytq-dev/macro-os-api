namespace Application;

/// <summary>WP1: các lần nhập gần nhất (mới nhất trước).</summary>
public sealed record GetFoodImportRunsQuery(int Top = 20) : IRequest<IReadOnlyList<FoodImportRunModel>>;
