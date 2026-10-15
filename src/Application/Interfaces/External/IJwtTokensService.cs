using System.Security.Claims;
namespace Application;

public interface IJwtTokensService
{
    SignInResponse GenerateAccessToken(UserCredentials user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

    // Token for email verification
    string GenerateEmailVerifyToken();
}

