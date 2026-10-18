using System.Security.Claims;
namespace Application;

public interface IJwtTokensService
{
    SignInResponse GenerateAccessToken(UserCredentials user);
    string GenerateRefreshToken();

    /// <summary>Thời điểm hết hạn của refresh token phát ra bây giờ (JwtOptions.RefreshTokenExpirationDays): dùng chung cho token, cột User.RefreshTokenExpiry và cookie.</summary>
    DateTimeOffset GetRefreshTokenExpiry();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

    // Token for email verification
    string GenerateEmailVerifyToken();
}

