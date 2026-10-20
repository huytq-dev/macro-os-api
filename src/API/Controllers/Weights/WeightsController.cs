namespace API;

/// <summary>Cân nặng theo ngày: ghi/ghi đè, xem danh sách kèm trend, xoá.</summary>
[Route("api/weights")]
public sealed class WeightsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Danh sách cân nặng kèm trend trong khoảng ngày (mặc định 90 ngày), mới nhất trước.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WeightEntriesResponse>>> GetList(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<WeightEntriesResponse>.Ok(await Sender.Send(new GetWeightEntriesQuery(from, to), ct)));

    /// <summary>
    /// Trang Habits (Weigh-In): ngày có cân/% mỡ của một tháng (lịch, `year`+`month`) hoặc cả năm (heatmap, chỉ `year`; mặc định năm hiện tại), kèm streak hiện tại/dài nhất,
    /// tổng số ngày đã cân và hôm nay theo múi giờ của user. `days` chỉ có ngày có dữ liệu.
    /// </summary>
    [HttpGet("habit")]
    public async Task<ActionResult<ApiResponse<WeighInHabitResponse>>> Habit([FromQuery] GetWeighInHabitQuery query, CancellationToken ct)
        => Ok(ApiResponse<WeighInHabitResponse>.Ok(await Sender.Send(query, ct)));

    /// <summary>Ghi cân nặng theo ngày (mặc định hôm nay); đã có thì ghi đè. Tính lại trend sau khi lưu.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Upsert(
        [FromBody] UpsertWeightEntryCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Xoá cân nặng của một ngày; tính lại trend.</summary>
    [HttpDelete("{logDate}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(DateOnly logDate, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteWeightEntryCommand(logDate), ct)));
}
