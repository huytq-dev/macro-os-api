using System.Text.Json.Serialization;

namespace Contract;

public sealed class SignInResponse
{
    public required string AccessToken { get; set; }
    public required int ExpiresIn { get; set; }
    
    // Không gửi Refreshtoken lên client
    [JsonIgnore]
    public string? RefreshToken { get; set; }
}
