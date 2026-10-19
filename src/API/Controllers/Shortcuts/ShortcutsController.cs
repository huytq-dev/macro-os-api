namespace API;

/// <summary>Cấu hình nút "+" ở thanh nav: thứ tự/bật tắt/màu shortcut và kiểu toolbar, đồng bộ nhiều thiết bị.</summary>
[Route("api/shortcuts")]
public sealed class ShortcutsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Cấu hình hiện tại; chưa lưu lần nào thì trả mặc định của BE (isDefault = true).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ShortcutSettingsModel>>> Get(CancellationToken ct)
        => Ok(ApiResponse<ShortcutSettingsModel>.Ok(await Sender.Send(new GetShortcutsQuery(), ct)));

    /// <summary>Thay toàn bộ cấu hình (idempotent); khoá catalog còn thiếu được bổ sung ở cuối, tắt.</summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<ShortcutSettingsModel>>> Update(
        [FromBody] UpdateShortcutsCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<ShortcutSettingsModel>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Đặt lại về mặc định của ứng dụng.</summary>
    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<ShortcutSettingsModel>>> Reset(CancellationToken ct)
        => Ok(ApiResponse<ShortcutSettingsModel>.Ok(await Sender.Send(new ResetShortcutsCommand(), ct)));
}
