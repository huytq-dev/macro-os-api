namespace API;

[Route("api/food-logs")]
public sealed class FoodLogsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Log của một ngày (mặc định hôm nay): entries theo bữa, tổng ngày, target tuần hiệu lực.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DayLogResponse>>> GetDay([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<DayLogResponse>.Ok(await Sender.Send(new GetDayLogQuery(date), ct)));

    /// <summary>Xoá toàn bộ log của ngày → thuật toán coi ngày này là chưa log.</summary>
    [HttpDelete("{date}")]
    public async Task<ActionResult<ApiResponse<string>>> ClearDay(DateOnly date, CancellationToken ct)
    {
        await Sender.Send(new ClearDayLogCommand(date), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("FoodLog")));
    }

    /// <summary>Thêm món vào bữa; snapshot macro tại thời điểm log.</summary>
    [HttpPost("entries")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddEntry([FromBody] AddFoodLogEntryCommand command, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(command, ct), statusCode: 201));

    /// <summary>Sửa bữa/giờ/serving/số lượng của một entry; tính lại tổng ngày.</summary>
    [HttpPut("entries/{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateEntry(Guid id, [FromBody] UpdateFoodLogEntryCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("FoodLogEntry")));
    }

    /// <summary>Xoá (soft) một entry; tính lại tổng ngày.</summary>
    [HttpDelete("entries/{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteEntry(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteFoodLogEntryCommand(id), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("FoodLogEntry")));
    }

    /// <summary>Quick add: nhập thẳng kcal/P/C/F cho một bữa.</summary>
    [HttpPost("quick-add")]
    public async Task<ActionResult<ApiResponse<Guid>>> QuickAdd([FromBody] QuickAddCommand command, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(command, ct), statusCode: 201));

    /// <summary>Copy bữa (mealType) hoặc cả ngày (mealType null) sang ngày khác; trả số entry đã copy.</summary>
    [HttpPost("copy")]
    public async Task<ActionResult<ApiResponse<int>>> Copy([FromBody] CopyFoodLogCommand command, CancellationToken ct)
        => Ok(ApiResponse<int>.Ok(await Sender.Send(command, ct)));
}
