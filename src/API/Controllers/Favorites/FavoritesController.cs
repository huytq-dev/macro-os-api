namespace API;

/// <summary>Món yêu thích (preset món + khẩu phần + số lượng): xem, lưu, bỏ, sắp xếp lại.</summary>
[Route("api/food-favorites")]
public sealed class FavoritesController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Danh sách preset yêu thích theo thứ tự đã sắp xếp, kèm kcal/macro đã tính theo lượng preset.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FavoriteFoodModel>>>> GetAll(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<FavoriteFoodModel>>.Ok(await Sender.Send(new GetFavoritesQuery(), ct)));

    /// <summary>Lưu preset (món + khẩu phần + số lượng); đã có thì trả lại id cũ.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Add(
        [FromBody] AddFavoriteCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Bỏ một preset yêu thích.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Remove(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new RemoveFavoriteCommand(id), ct)));

    /// <summary>Sắp xếp lại theo thứ tự danh sách id gửi lên.</summary>
    [HttpPut("order")]
    public async Task<ActionResult<ApiResponse<Unit>>> Reorder(
        [FromBody] ReorderFavoritesCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
