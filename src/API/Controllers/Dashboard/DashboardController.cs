namespace API;

/// <summary>Dashboard: layout tuỳ chỉnh (thứ tự, ẩn/hiện widget).</summary>
[Route("api/dashboard")]
public sealed class DashboardController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Layout dashboard của user; chưa tuỳ chỉnh thì trả mặc định (không ghi DB).</summary>
    [HttpGet("layout")]
    public async Task<ActionResult<ApiResponse<DashboardLayoutResponse>>> GetLayout(CancellationToken ct)
        => Ok(ApiResponse<DashboardLayoutResponse>.Ok(await Sender.Send(new GetDashboardLayoutQuery(), ct)));

    /// <summary>Thay toàn bộ layout; widget vắng mặt về mặc định, danh sách rỗng = reset hết.</summary>
    [HttpPut("layout")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateLayout(
        [FromBody] UpdateDashboardLayoutCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
