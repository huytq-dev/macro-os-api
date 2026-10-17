namespace API;

/// <summary>Chu kỳ kinh: log, sửa, xoá; độ dài chu kỳ tự tính lại.</summary>
[Route("api/periods")]
public sealed class PeriodsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Các kỳ kinh (mới nhất trước), mặc định 365 ngày; kèm kỳ gần nhất và số ngày kể từ đó.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PeriodLogsResponse>>> GetList(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<PeriodLogsResponse>.Ok(await Sender.Send(new GetPeriodLogsQuery(from, to), ct)));

    /// <summary>Thêm kỳ kinh; EndDate null = đang trong kỳ.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreatePeriodLogCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa kỳ kinh (đổi ngày, đóng kỳ, triệu chứng/ghi chú); tính lại độ dài chu kỳ.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(
        Guid id, [FromBody] UpdatePeriodLogCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá kỳ kinh; tính lại độ dài chu kỳ các kỳ còn lại.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeletePeriodLogCommand(id), ct)));
}
