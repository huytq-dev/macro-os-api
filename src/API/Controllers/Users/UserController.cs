namespace API;

[Route("api/users")]
public sealed class UserController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Lấy hồ sơ của người dùng đang đăng nhập.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<GetProfileResponse>>> GetProfile(CancellationToken ct)
        => Ok(ApiResponse<GetProfileResponse>.Ok(await Sender.Send(new GetProfileQuery(), ct)));

    /// <summary>Cập nhật hồ sơ của người dùng đang đăng nhập (trường null = giữ nguyên).</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateProfile([FromBody] UpdateProfileCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Profile")));
    }
}
