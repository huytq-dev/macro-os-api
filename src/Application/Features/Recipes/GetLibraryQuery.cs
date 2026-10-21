namespace Application;

/// <summary>
/// WP4 / tab Library: món tự tạo của tôi (không gồm quick add ẩn) + recipe của tôi, theo tên, phân trang. Calories/macro tính theo khẩu phần mặc định
/// (món) hoặc 1 khẩu phần (recipe). Một truy vấn UNION cho trang + một truy vấn lấy khẩu phần mặc định của các món trong trang.
/// </summary>
public sealed record GetLibraryQuery(string? Search, LibraryScope Scope = LibraryScope.All, int Top = AppConstants.DefaultPageTop, int Skip = AppConstants.DefaultPageSkip)
    : IRequest<QueryResult<LibraryItemModel>>;
