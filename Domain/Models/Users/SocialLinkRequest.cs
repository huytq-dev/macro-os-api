namespace Contract;

public sealed class SocialLinkRequest
{
    public int Platform { get; set; }
    public required string Url { get; set; }
}
