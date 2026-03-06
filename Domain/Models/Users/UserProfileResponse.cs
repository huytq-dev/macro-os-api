namespace Contract;

public sealed class UserProfileResponse
{
    public Guid Id { get; init; }
    public required string FullName { get; init; } 
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }     
    public string? Major { get; init; }
    public string? AvatarUrl { get; init; }        
    // FPT/Student Info
    public string? StudentId { get; init; }
    public string? UniversityId { get; init; }  
    public string? SchoolName { get; init; }
    public string? ClassName { get; init; }
    // Demographics
    public string? Gender { get; init; }          
    public DateTime? DOB { get; init; }
    public string? Address { get; init; }
    public List<SocialLinkResponse> SocialLinks { get; init; } = new();
    // System Status
    public bool IsVerified { get; init; }
    // Roles
    public string RoleName { get; init; } = "PARTICIPANT";

}
