namespace Contract;

public sealed class SignOutRequest
{
    public Guid UserId { get; set; }
    public string? AccessToken { get; set; }
}