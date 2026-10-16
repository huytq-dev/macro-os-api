namespace API;

/// <summary>Hồ sơ của người dùng đang đăng nhập.</summary>
[Route("api/users")]
public sealed class UserController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Lấy hồ sơ của người dùng đang đăng nhập.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<GetProfileResponse>>> GetProfile(CancellationToken ct)
        => Ok(ApiResponse<GetProfileResponse>.Ok(await Sender.Send(new GetProfileQuery(), ct)));

    /// <summary>Cập nhật hồ sơ của người dùng đang đăng nhập (trường null = giữ nguyên).</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateProfile(
        [FromBody] UpdateProfileCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
