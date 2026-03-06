namespace Contract;

public sealed class ConfirmEmailRequest
{
    public required string Token { get; set; }
}