using System.Text.Json;

namespace Application;

/// <summary>WP4: danh sách recipe của mình (theo tên), có tìm kiếm và phân trang.</summary>
public sealed record GetRecipesQuery(string? Search, int Top = AppConstants.DefaultPageTop, int Skip = AppConstants.DefaultPageSkip)
    : IRequest<QueryResult<RecipeSummaryModel>>;
