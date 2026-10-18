namespace API;

/// <summary>Goal cân nặng: tiến độ (waterfall/dial, ETA) và cân nặng mục tiêu.</summary>
[Route("api/goals")]
public sealed class GoalsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Tiến độ goal đang hoạt động: % hoàn thành hoặc độ lệch, cột theo tuần, ngày dự kiến.</summary>
    [HttpGet("progress")]
    public async Task<ActionResult<ApiResponse<GoalProgressResponse>>> GetProgress(CancellationToken ct)
        => Ok(ApiResponse<GoalProgressResponse>.Ok(await Sender.Send(new GetGoalProgressQuery(), ct)));

    /// <summary>Lịch sử mọi goal của tôi (mới nhất trước): loại, cân nặng bắt đầu/mục tiêu, tốc độ, khoảng ngày và trạng thái.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GoalHistoryItemModel>>>> GetHistory(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<GoalHistoryItemModel>>.Ok(await Sender.Send(new GetGoalHistoryQuery(), ct)));

    /// <summary>Đặt hoặc xoá cân nặng mục tiêu của goal đang hoạt động.</summary>
    [HttpPut("target")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateTarget(
        [FromBody] UpdateGoalTargetCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
