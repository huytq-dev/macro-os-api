namespace API;

/// <summary>Số đo cơ thể: hiện hỗ trợ % mỡ (tile Visual Body Fat).</summary>
[Route("api/body-metrics")]
public sealed class BodyMetricsController(ISender sender) : BaseApiController(sender)
{
    // --- Body fat ---

    /// <summary>% mỡ mới nhất trước; lọc theo phương pháp đo (Visual, Scale…), mặc định 7 lần gần nhất.</summary>
    [HttpGet("body-fat")]
    public async Task<ActionResult<ApiResponse<BodyFatResponse>>> GetBodyFat(
        [FromQuery] MeasurementMethod? method, [FromQuery] int? limit, CancellationToken ct)
        => Ok(ApiResponse<BodyFatResponse>.Ok(await Sender.Send(new GetBodyFatQuery(method, limit), ct)));

    /// <summary>Ghi % mỡ (mặc định hôm nay); cùng ngày + cùng phương pháp thì ghi đè.</summary>
    [HttpPost("body-fat")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpsertBodyFat(
        [FromBody] UpsertBodyFatCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    // --- Full body (sheet Metrics) ---

    /// <summary>Sheet Metrics của một ngày (mặc định hôm nay): % mỡ Visual + số đo vòng, để điền sẵn.</summary>
    [HttpGet("full-body")]
    public async Task<ActionResult<ApiResponse<FullBodyResponse>>> GetFullBody([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<FullBodyResponse>.Ok(await Sender.Send(new GetFullBodyQuery(date), ct)));

    /// <summary>Lưu cả sheet Metrics trong một lần (hàng Visual + hàng Tape), thay thế trạng thái của ngày; ô null bị xoá.</summary>
    [HttpPut("full-body")]
    public async Task<ActionResult<ApiResponse<FullBodyResponse>>> UpsertFullBody(
        [FromBody] UpsertFullBodyCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<FullBodyResponse>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Xoá sheet Metrics của một ngày (mặc định hôm nay): cả % mỡ Visual lẫn số đo vòng.</summary>
    [HttpDelete("full-body")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteFullBody([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteFullBodyCommand(date), ct)));

    // --- Chung ---

    /// <summary>Xoá một lần đo (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteBodyMetricCommand(id), ct)));
}
