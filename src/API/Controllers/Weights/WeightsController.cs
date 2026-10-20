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
