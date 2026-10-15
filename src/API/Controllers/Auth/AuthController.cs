namespace API;

[Route("api/auth")]
public sealed class AuthController(ISender sender) : BaseApiController(sender)
{
    private const string RefreshTokenCookie = "refreshToken";

    /// <summary>Đăng ký tài khoản mới và gửi email xác thực.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] RegisterCommand command, CancellationToken ct)
        => Ok(ApiResponse<string>.Ok(await Sender.Send(command, ct), statusCode: 201));

    /// <summary>Đăng nhập bằng email/mật khẩu, nhận access token; refresh token đặt vào HttpOnly cookie.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SignInResponse>>> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        SetRefreshTokenCookie(result.RefreshToken!);
        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    /// <summary>Làm mới access token bằng refresh token trong cookie (hoặc body).</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SignInResponse>>> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var fromCookie = Request.Cookies[RefreshTokenCookie];
        var result = await Sender.Send(command with { RefreshToken = command.RefreshToken ?? fromCookie }, ct);
        SetRefreshTokenCookie(result.RefreshToken!);
        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    /// <summary>Đăng xuất: huỷ refresh token của phiên hiện tại.</summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<string>>> Logout(CancellationToken ct)
    {
        await Sender.Send(new SignOutCommand(), ct);
        Response.Cookies.Delete(RefreshTokenCookie);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("Session")));
    }

    /// <summary>Gửi email đặt lại mật khẩu (luôn trả 200 để không lộ email có tồn tại).</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage("Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu."));
    }

    /// <summary>Đặt lại mật khẩu bằng token nhận qua email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Password")));
    }

    /// <summary>Đổi mật khẩu tài khoản hiện tại.</summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Password")));
    }

    /// <summary>Xác thực email bằng token nhận qua email.</summary>
    [HttpPost("confirm-email")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage("Email đã được xác thực thành công."));
    }

    // RefreshToken là [JsonIgnore] trên SignInResponse — chỉ đi qua HttpOnly cookie, không lộ ra body
    private void SetRefreshTokenCookie(string refreshToken)
        => Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(30)
        });
}
