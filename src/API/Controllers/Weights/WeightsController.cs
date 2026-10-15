namespace API;

[Route("api/weights")]
public sealed class WeightsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Ghi cân nặng theo ngày (mặc định hôm nay); đã có thì ghi đè. Tính lại trend sau khi lưu.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Upsert([FromBody] UpsertWeightEntryCommand command, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(command, ct), statusCode: 201));

    /// <summary>Danh sách cân nặng kèm trend trong khoảng ngày (mặc định 90 ngày), mới nhất trước.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WeightEntriesResponse>>> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<WeightEntriesResponse>.Ok(await Sender.Send(new GetWeightEntriesQuery(from, to), ct)));

    /// <summary>Xoá cân nặng của một ngày; tính lại trend.</summary>
    [HttpDelete("{logDate}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(DateOnly logDate, CancellationToken ct)
    {
        await Sender.Send(new DeleteWeightEntryCommand(logDate), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("WeightEntry")));
    }
}
