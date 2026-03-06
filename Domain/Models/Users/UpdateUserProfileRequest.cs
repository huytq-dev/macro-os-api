namespace Contract;

public sealed class UpdateUserProfileRequest
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Major { get; set; }
    public string? Address { get; set; }
    public DateTime? DOB { get; set; }
    public string? Gender { get; set; }
    public List<SocialLinkRequest>? SocialLinks { get; set; }
}
