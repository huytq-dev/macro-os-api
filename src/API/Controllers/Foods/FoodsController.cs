namespace API;

[Route("api/foods")]
public sealed class FoodsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Tìm thực phẩm theo tên/brand, lọc category và phạm vi (All/Mine/System), phân trang top/skip.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QueryResult<FoodItemModel>>>> Search([FromQuery] SearchFoodsQuery query, CancellationToken ct)
        => Ok(ApiResponse<QueryResult<FoodItemModel>>.Ok(await Sender.Send(query, ct)));

    /// <summary>Chi tiết thực phẩm: dinh dưỡng /100 g và danh sách serving.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FoodItemResponse>>> Get(Guid id, CancellationToken ct)
        => Ok(ApiResponse<FoodItemResponse>.Ok(await Sender.Send(new GetFoodQuery(id), ct)));

    /// <summary>Tạo custom food của người dùng hiện tại.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateCustomFoodCommand command, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(command, ct), statusCode: 201));

    /// <summary>Sửa custom food do mình tạo (servings gửi lên = thay toàn bộ).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(Guid id, [FromBody] UpdateCustomFoodCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("FoodItem")));
    }

    /// <summary>Xoá (soft) custom food do mình tạo; log cũ giữ snapshot.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCustomFoodCommand(id), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("FoodItem")));
    }
}
