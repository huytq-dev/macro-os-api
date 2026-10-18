using System.Text.Json.Serialization;

namespace Contract;

public sealed class SignInResponse
{
    public required string AccessToken { get; set; }
    public required int ExpiresIn { get; set; }
    
    // Không gửi Refreshtoken lên client
    [JsonIgnore]
    public string? RefreshToken { get; set; }

    // Hạn của refresh token (cùng giá trị với User.RefreshTokenExpiry); controller dùng đặt hạn cookie, không gửi lên client
    [JsonIgnore]
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}
