namespace API;

/// <summary>Thư viện của tôi: món tự tạo + recipe.</summary>
[Route("api/library")]
public sealed class LibraryController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Món tự tạo và recipe của tôi theo tên (scope All/Foods/Recipes), tìm kiếm và phân trang top/skip.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QueryResult<LibraryItemModel>>>> List(
        [FromQuery] GetLibraryQuery query, CancellationToken ct)
        => Ok(ApiResponse<QueryResult<LibraryItemModel>>.Ok(await Sender.Send(query, ct)));
}
