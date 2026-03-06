namespace Contract;
public sealed class ResetPasswordRequest
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}
