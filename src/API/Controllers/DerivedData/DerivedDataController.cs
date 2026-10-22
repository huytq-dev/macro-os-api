namespace API;

/// <summary>Trạng thái dựng lại dữ liệu dẫn xuất (snapshot expenditure/daily progress): BE là nguồn sự thật cho "đang cập nhật" hay "đã mới".</summary>
[Route("api/derived-data")]
public sealed class DerivedDataController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Ready (số khớp dữ liệu nguồn) | Updating (còn job dựng lại, FE hiện "đang cập nhật" và refetch theo nhịp) | Failed (hết lượt thử, cho thử lại).</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<DerivedDataStatusResponse>>> GetStatus(CancellationToken ct)
        => Ok(ApiResponse<DerivedDataStatusResponse>.Ok(await Sender.Send(new GetDerivedDataStatusQuery(), ct)));

    /// <summary>Thử lại các job Failed (hết lượt thử); trả trạng thái mới.</summary>
    [HttpPost("retry")]
    public async Task<ActionResult<ApiResponse<DerivedDataStatusResponse>>> Retry(CancellationToken ct)
        => Ok(ApiResponse<DerivedDataStatusResponse>.Ok(await Sender.Send(new RetryDerivedDataCommand(), ct)));
}
