namespace API;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender _sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var message = await _sender.Send(command, ct);
        return Ok(ApiResponse<string>.Ok(message, statusCode: 201));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        // RefreshToken là [JsonIgnore] — set vào HTTP-only cookie, không expose ra response body
        Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return Ok(ApiResponse<SignInResponse>.Ok(result));
    }

    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _sender.Send(new SignOutCommand(userId), ct);

        Response.Cookies.Delete("refreshToken");

        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.DeletedSuccessfully("Session")));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        // Luôn trả 200 để không tiết lộ email có tồn tại hay không
        return Ok(ApiResponse<string>.OkMessage("Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Password")));
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _sender.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Password")));
    }

    [HttpPost("confirm-email")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _sender.Send(new ConfirmEmailCommand(userId, request.Token), ct);
        return Ok(ApiResponse<string>.OkMessage("Email đã được xác thực thành công."));
    }
}
