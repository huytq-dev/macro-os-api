namespace API;

/// <summary>Check-in hằng tuần: xem đề xuất, chấp nhận/từ chối, đổi ngày check-in, lịch sử.</summary>
[Route("api/check-ins")]
public sealed class CheckInsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Lịch sử check-in (mới nhất trước).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CheckInHistoryModel>>>> GetHistory(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<CheckInHistoryModel>>.Ok(await Sender.Send(new GetCheckInHistoryQuery(), ct)));

    /// <summary>Xem tuần qua và đề xuất target tuần tới (ALG-04); IsDue = đã tới ngày check-in (sớm ≤ 2 ngày).</summary>
    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<CheckInPreviewResponse>>> GetPreview(CancellationToken ct)
        => Ok(ApiResponse<CheckInPreviewResponse>.Ok(await Sender.Send(new GetCheckInPreviewQuery(), ct)));

    /// <summary>Chấp nhận đề xuất: tạo target tuần mới từ hôm nay.</summary>
    [HttpPost("accept")]
    public async Task<ActionResult<ApiResponse<Guid>>> Accept(CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(new AcceptCheckInCommand(), ct)));

    /// <summary>Từ chối đề xuất: giữ target cũ, ghi nhận check-in.</summary>
    [HttpPost("reject")]
    public async Task<ActionResult<ApiResponse<Guid>>> Reject(CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(new RejectCheckInCommand(), ct)));

    /// <summary>Đổi ngày check-in trong tuần.</summary>
    [HttpPut("weekday")]
    public async Task<ActionResult<ApiResponse<Unit>>> ChangeWeekday(
        [FromBody] ChangeCheckInWeekdayCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
