namespace API;

/// <summary>Cân nặng theo ngày: ghi/ghi đè, xem danh sách kèm trend, xoá.</summary>
[Route("api/weights")]
public sealed class WeightsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>
    /// Danh sách cân nặng kèm trend và % mỡ cân từng ngày trong khoảng (mặc định 90 ngày tới hôm nay theo múi giờ user; `all=true` = từ ngày có cân đầu tiên), mới nhất trước,
    /// cùng thống kê khoảng (`summary`: average, difference) và, khi có `granularity` (Day | Week | Month), trung bình mỗi bucket (`buckets`). `limit` chỉ cắt `items` (vd 7 lần cân gần nhất cho Dashboard).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WeightEntriesResponse>>> GetList(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] MetricGranularity? granularity, [FromQuery] bool? all, [FromQuery] int? limit, CancellationToken ct)
        => Ok(ApiResponse<WeightEntriesResponse>.Ok(await Sender.Send(new GetWeightEntriesQuery(from, to, granularity, all, limit), ct)));

    /// <summary>
    /// Trang Habits (Weigh-In): ngày có cân/% mỡ của một tháng (lịch, `year`+`month`) hoặc cả năm (heatmap, chỉ `year`; mặc định năm hiện tại), kèm streak hiện tại/dài nhất,
    /// tổng số ngày đã cân và hôm nay theo múi giờ của user. `days` chỉ có ngày có dữ liệu.
    /// </summary>
    [HttpGet("habit")]
    public async Task<ActionResult<ApiResponse<WeighInHabitResponse>>> Habit([FromQuery] GetWeighInHabitQuery query, CancellationToken ct)
        => Ok(ApiResponse<WeighInHabitResponse>.Ok(await Sender.Send(query, ct)));

    /// <summary>
    /// Trang Weight Trend: trend MỖI NGÀY từ lần cân đầu tiên tới To (mặc định và tối đa hôm nay theo múi giờ user; From mặc định 90 ngày trước To, `all=true` = từ lần cân đầu), tăng dần theo ngày,
    /// mỗi ngày kèm cân thực (null nếu không cân) và thay đổi so với hôm trước; `summary` (average, difference theo trend), `scaleSummary` (theo số cân thực) và `buckets` khi có `granularity` (Day | Week | Month).
    /// </summary>
    [HttpGet("trend")]
    public async Task<ActionResult<ApiResponse<WeightTrendResponse>>> GetTrend(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool? all, [FromQuery] MetricGranularity? granularity, CancellationToken ct)
        => Ok(ApiResponse<WeightTrendResponse>.Ok(await Sender.Send(new GetWeightTrendQuery(from, to, all, granularity), ct)));

    /// <summary>Insights &amp; Data của Weight Trend: thay đổi 3/7/14/30/90 ngày, cân hiện tại (trend), thay đổi hằng tuần, thâm hụt năng lượng, cân dự kiến sau 30 ngày.</summary>
    [HttpGet("trend/insights")]
    public async Task<ActionResult<ApiResponse<WeightTrendInsightsResponse>>> GetTrendInsights(CancellationToken ct)
        => Ok(ApiResponse<WeightTrendInsightsResponse>.Ok(await Sender.Send(new GetWeightTrendInsightsQuery(), ct)));

    /// <summary>Ghi cân nặng theo ngày (mặc định hôm nay theo múi giờ user, không ở tương lai) kèm % mỡ cân tuỳ chọn; đã có thì ghi đè. Tính lại trend sau khi lưu.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Upsert(
        [FromBody] UpsertWeightEntryCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Xoá cân nặng của một ngày (kèm % mỡ cân của ngày đó); tính lại trend.</summary>
    [HttpDelete("{logDate}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(DateOnly logDate, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteWeightEntryCommand(logDate), ct)));
}
