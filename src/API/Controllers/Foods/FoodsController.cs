namespace API;

/// <summary>Thư viện thực phẩm: tìm kiếm, xem chi tiết, tạo/sửa/xoá thực phẩm tự tạo của người dùng.</summary>
[Route("api/foods")]
public sealed class FoodsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Tìm thực phẩm theo tên/brand, lọc category và phạm vi (All/Mine/System), phân trang top/skip.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QueryResult<FoodItemModel>>>> Search(
        [FromQuery] SearchFoodsQuery query, CancellationToken ct)
        => Ok(ApiResponse<QueryResult<FoodItemModel>>.Ok(await Sender.Send(query, ct)));

    /// <summary>Chi tiết thực phẩm: dinh dưỡng /100 g và danh sách serving.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FoodItemResponse>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<FoodItemResponse>.Ok(await Sender.Send(new GetFoodQuery(id), ct)));

    /// <summary>Tạo custom food của người dùng hiện tại.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateCustomFoodCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa custom food do mình tạo (servings gửi lên = thay toàn bộ).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(
        Guid id, [FromBody] UpdateCustomFoodCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá (soft) custom food do mình tạo; log cũ giữ snapshot.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteCustomFoodCommand(id), ct)));
}
