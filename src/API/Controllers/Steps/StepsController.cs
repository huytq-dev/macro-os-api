namespace API;

/// <summary>Số bước theo ngày: nhập tay, xem danh sách, xoá (chỉ hiển thị, không vào expenditure).</summary>
[Route("api/steps")]
public sealed class StepsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Số bước theo ngày (mới nhất trước), mặc định 30 ngày; kèm trung bình 7 ngày cuối.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StepEntriesResponse>>> GetList(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<StepEntriesResponse>.Ok(await Sender.Send(new GetStepEntriesQuery(from, to), ct)));

    /// <summary>Ghi số bước một ngày (mặc định hôm nay); đã có thì ghi đè.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Upsert(
        [FromBody] UpsertStepEntryCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Xoá số bước của một ngày.</summary>
    [HttpDelete("{logDate}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(DateOnly logDate, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteStepEntryCommand(logDate), ct)));
}
