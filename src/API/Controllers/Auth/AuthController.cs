namespace API;

/// <summary>Xác thực tài khoản: đăng ký, đăng nhập, làm mới token, đăng xuất, quên/đặt lại/đổi mật khẩu, xác thực email.</summary>
[Route("api/auth")]
public sealed class AuthController(ISender sender) : BaseApiController(sender)
{
    private const string RefreshTokenCookie = "refreshToken";

    /// <summary>Đăng ký tài khoản mới và gửi email xác thực.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> Register(
        [FromBody] RegisterCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<string>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Đăng nhập bằng email/mật khẩu, nhận access token; refresh token đặt vào HttpOnly cookie.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SignInResponse>>> Login(
        [FromBody] LoginCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        SetRefreshTokenCookie(result);
        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    /// <summary>Làm mới access token bằng refresh token trong cookie (hoặc body); refresh token sai/hết hạn trả 401 và xoá cookie.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SignInResponse>>> RefreshToken(
        [FromBody] RefreshTokenCommand cmd, CancellationToken ct)
    {
        // Lỗi 401 do middleware xử lý chung (controller không bắt exception): xoá cookie refresh hỏng ngay trước khi gửi header để client không gửi lại mãi
        Response.OnStarting(() =>
        {
            if (Response.StatusCode == StatusCodes.Status401Unauthorized) Response.Cookies.Delete(RefreshTokenCookie, CookieOptionsFor(null));
            return Task.CompletedTask;
        });

        var fromCookie = Request.Cookies[RefreshTokenCookie];
        var result = await Sender.Send(cmd with { RefreshToken = cmd.RefreshToken ?? fromCookie }, ct);
        SetRefreshTokenCookie(result);
        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    /// <summary>Đăng xuất: huỷ refresh token của phiên hiện tại.</summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<Unit>>> Logout(CancellationToken ct)
    {
        var result = await Sender.Send(new SignOutCommand(), ct);
        Response.Cookies.Delete(RefreshTokenCookie);
        return Ok(ApiResponse<Unit>.Ok(result));
    }

    /// <summary>Gửi email đặt lại mật khẩu (luôn trả 200 để không lộ email có tồn tại).</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Unit>>> ForgotPassword(
        [FromBody] ForgotPasswordCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct), "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu."));

    /// <summary>Đặt lại mật khẩu bằng token nhận qua email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Unit>>> ResetPassword(
        [FromBody] ResetPasswordCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Đổi mật khẩu tài khoản hiện tại.</summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<Unit>>> ChangePassword(
        [FromBody] ChangePasswordCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Xác thực email bằng token trong link gửi qua email (không cần đăng nhập).</summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Unit>>> ConfirmEmail(
        [FromBody] ConfirmEmailCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct), "Email đã được xác thực thành công."));

    /// <summary>Gửi lại email xác thực (luôn trả 200 để không lộ email có tồn tại; tối đa 1 email/60 giây).</summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Unit>>> ResendVerification(
        [FromBody] ResendVerificationCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct), "Nếu email tồn tại và chưa xác thực, bạn sẽ nhận được email mới."));

    // RefreshToken là [JsonIgnore] trên SignInResponse — chỉ đi qua HttpOnly cookie, không lộ ra body
    private void SetRefreshTokenCookie(SignInResponse result)
        => Response.Cookies.Append(RefreshTokenCookie, result.RefreshToken!, CookieOptionsFor(result.RefreshTokenExpiresAt));

    // Secure theo giao thức TRÌNH DUYỆT đang dùng: Next rewrite gọi BE bằng https nên Request.IsHttps luôn true, còn Safari trên http://IP-LAN
    // từ chối lưu cookie Secure. Phía sau proxy lấy X-Forwarded-Proto (Next/nginx đặt); không có header thì dùng Request.IsHttps.
    private CookieOptions CookieOptionsFor(DateTimeOffset? expires)
    {
        var proto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault()?.Split(',')[0].Trim();
        return new CookieOptions
        {
            HttpOnly = true,
            Secure   = proto is not null ? proto.Equals("https", StringComparison.OrdinalIgnoreCase) : Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires  = expires,
        };
    }
}
