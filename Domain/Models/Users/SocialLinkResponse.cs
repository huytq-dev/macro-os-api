namespace Contract;

public sealed class SocialLinkResponse
{
    public int Platform { get; init; }
    public required string Url { get; init; }
}
