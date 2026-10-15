namespace API;

[Route("api/auth")]
public sealed class AuthController(ISender sender) : BaseApiController(sender)
{
    private const string RefreshTokenCookie = "refreshToken";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var message = await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.Ok(message, statusCode: 201));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        SetRefreshTokenCookie(result.RefreshToken!);
        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        SetRefreshTokenCookie(result.RefreshToken!);
        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await Sender.Send(new SignOutCommand(User.GetUserId()), ct);
        Response.Cookies.Delete(RefreshTokenCookie);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("Session")));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        // Luôn trả 200 để không tiết lộ email có tồn tại hay không
        return Ok(ApiResponse<string>.OkMessage("Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Password")));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await Sender.Send(new ChangePasswordCommand(User.GetUserId(), request.CurrentPassword, request.NewPassword, request.ConfirmPassword), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Password")));
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        await Sender.Send(new ConfirmEmailCommand(User.GetUserId(), request.Token), ct);
        return Ok(ApiResponse<string>.OkMessage("Email đã được xác thực thành công."));
    }

    // RefreshToken là [JsonIgnore] trên SignInResponse — chỉ đi qua HTTP-only cookie, không lộ ra body
    private void SetRefreshTokenCookie(string refreshToken)
        => Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(30)
        });
}
