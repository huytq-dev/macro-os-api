namespace Contract;

public sealed class RefreshTokenRequest
{
    public required Guid UserId { get; set; }
    public required string RefreshToken { get; set; }
}