namespace API;

/// <summary>Số đo cơ thể: hiện hỗ trợ % mỡ (tile Visual Body Fat).</summary>
[Route("api/body-metrics")]
public sealed class BodyMetricsController(ISender sender) : BaseApiController(sender)
{
    // --- Body fat ---

    /// <summary>
    /// % mỡ mới nhất trước; lọc theo phương pháp đo (Visual, Scale…). Không from/to: 7 lần gần nhất (limit ≤ 100). Có from và/hoặc to: MỌI dòng trong khoảng (gồm hai đầu, bỏ qua limit);
    /// latestPercent luôn là lần mới nhất của phương pháp đó.
    /// </summary>
    [HttpGet("body-fat")]
    public async Task<ActionResult<ApiResponse<BodyFatResponse>>> GetBodyFat(
        [FromQuery] MeasurementMethod? method, [FromQuery] int? limit, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<BodyFatResponse>.Ok(await Sender.Send(new GetBodyFatQuery(method, limit, from, to), ct)));

    /// <summary>Ghi % mỡ (mặc định hôm nay); cùng ngày + cùng phương pháp thì ghi đè.</summary>
    [HttpPost("body-fat")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpsertBodyFat(
        [FromBody] UpsertBodyFatCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    // --- Visual Body Fat (trang riêng) ---

    /// <summary>
    /// Trang Visual Body Fat: % mỡ Visual trong khoảng (mặc định 90 ngày tới hôm nay theo múi giờ user; `all=true` = từ lần ghi đầu), mới nhất trước, mỗi ngày một dòng,
    /// kèm thống kê khoảng (`summary`: average, difference) và, khi có `granularity` (Day | Week | Month), trung bình mỗi bucket.
    /// </summary>
    [HttpGet("visual-body-fat")]
    public async Task<ActionResult<ApiResponse<VisualBodyFatResponse>>> GetVisualBodyFat(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] MetricGranularity? granularity, [FromQuery] bool? all, [FromQuery] int? limit, CancellationToken ct)
        => Ok(ApiResponse<VisualBodyFatResponse>.Ok(await Sender.Send(new GetVisualBodyFatQuery(from, to, granularity, all, limit), ct)));

    /// <summary>Ghi % mỡ Visual của một ngày (mặc định hôm nay theo múi giờ user, không ở tương lai); ngày đã có thì ghi đè.</summary>
    [HttpPost("visual-body-fat")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpsertVisualBodyFat(
        [FromBody] UpsertVisualBodyFatCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Xoá % mỡ Visual của một ngày; không có → 404.</summary>
    [HttpDelete("visual-body-fat/{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteVisualBodyFat(DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteVisualBodyFatCommand(date), ct)));

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

    /// <summary>
    /// Lưu MỘT PHẦN của một ngày: chỉ các ô gửi lên (% mỡ Visual, số đo vòng) bị ghi/ghi đè, ô khác giữ nguyên; `clear` = khoá chỉ số cần xoá (waist, leftBicep, visualBodyFat…).
    /// Khác PUT full-body (thay cả ngày). Ngày mặc định hôm nay theo múi giờ user, không ở tương lai; trả lại cả ngày kèm cân/trend.
    /// </summary>
    [HttpPatch("full-body")]
    public async Task<ActionResult<ApiResponse<FullBodyResponse>>> PatchFullBody(
        [FromBody] PatchFullBodyCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<FullBodyResponse>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Các ngày có dữ liệu Full Body (mới nhất trước) kèm số chỉ số, % mỡ Visual, số đo vòng, cân và trend; from/to tuỳ chọn (gồm hai đầu).</summary>
    [HttpGet("full-body/history")]
    public async Task<ActionResult<ApiResponse<FullBodyHistoryResponse>>> GetFullBodyHistory(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<FullBodyHistoryResponse>.Ok(await Sender.Send(new GetFullBodyHistoryQuery(from, to), ct)));

    /// <summary>So sánh hai ngày: giá trị A/B, thay đổi (B − A), xu hướng và đánh giá Good/Bad theo mục tiêu của user.</summary>
    [HttpGet("full-body/compare")]
    public async Task<ActionResult<ApiResponse<FullBodyCompareResponse>>> CompareFullBody(
        [FromQuery] DateOnly? dateA, [FromQuery] DateOnly? dateB, CancellationToken ct)
        => Ok(ApiResponse<FullBodyCompareResponse>.Ok(await Sender.Send(new GetFullBodyCompareQuery(dateA, dateB), ct)));

    // --- Trang từng số đo vòng (neck, shoulders, bust, chest, waist, hips, leftBicep, rightBicep, leftForearm, rightForearm, leftWrist, rightWrist, leftThigh, rightThigh, leftCalf, rightCalf, leftAnkle, rightAnkle) ---

    /// <summary>Dashboard: các số đo vòng đã có dữ liệu, mỗi số đo kèm giá trị mới nhất và `entries` lần đo gần nhất (mặc định 7).</summary>
    [HttpGet("measurements/overview")]
    public async Task<ActionResult<ApiResponse<MeasurementOverviewResponse>>> GetMeasurementOverview(
        [FromQuery] int? entries, CancellationToken ct)
        => Ok(ApiResponse<MeasurementOverviewResponse>.Ok(await Sender.Send(new GetMeasurementOverviewQuery(entries), ct)));

    /// <summary>
    /// Trang một số đo: các lần đo trong khoảng (cm, mới nhất trước, mỗi ngày một giá trị; mặc định 90 ngày tới hôm nay, `all=true` = từ lần đo đầu), thống kê khoảng (`summary`)
    /// và bucket Day | Week | Month (`granularity`); `limit` chỉ cắt `items`. Chung nguồn dữ liệu với Full Body.
    /// </summary>
    [HttpGet("measurements/{key}")]
    public async Task<ActionResult<ApiResponse<MeasurementSeriesResponse>>> GetMeasurementSeries(
        string key, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] MetricGranularity? granularity, [FromQuery] bool? all, [FromQuery] int? limit, CancellationToken ct)
        => Ok(ApiResponse<MeasurementSeriesResponse>.Ok(await Sender.Send(new GetMeasurementSeriesQuery(key, from, to, granularity, all, limit), ct)));

    /// <summary>Ghi một số đo của một ngày (cm; mặc định hôm nay theo múi giờ user, không ở tương lai); ngày đã có thì ghi đè, số đo khác của ngày đó không đổi.</summary>
    [HttpPost("measurements/{key}")]
    public async Task<ActionResult<ApiResponse<MeasurementEntryModel>>> UpsertMeasurement(
        string key, [FromBody] UpsertMeasurementCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<MeasurementEntryModel>.Ok(await Sender.Send(cmd with { Key = key }, ct)));

    /// <summary>Xoá một số đo của một ngày (số đo khác của ngày đó giữ nguyên); không có → 404.</summary>
    [HttpDelete("measurements/{key}/{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteMeasurement(string key, DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteMeasurementCommand(key, date), ct)));

    // --- Chung ---

    /// <summary>Xoá một lần đo (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteBodyMetricCommand(id), ct)));
}
