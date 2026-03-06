using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure;

[RegisterService(typeof(IJwtTokensService))]
public class JwtTokenService : IJwtTokensService
{
    private readonly JwtOptions _jwt;
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IAppConfiguration appConfiguration)
    {
        _jwt = appConfiguration.GetJwtOptions();
        var keyBytes = Encoding.UTF8.GetBytes(_jwt.Key);
        var key = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public SignInResponse GenerateAccessToken(UserCredentials user)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName ?? string.Empty),
            new("role", user.Role ?? string.Empty),
            new("avatar", user.AvatarURL ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(_jwt.AccessTokenLifetime),
            signingCredentials: _signingCredentials
        );

        return new SignInResponse
        {
            AccessToken = _handler.WriteToken(token),
            ExpiresIn = _jwt.AccessTokenLifetime
        };
    }

    public string GenerateEmailVerifyToken()
           => TokenHelper.GenerateToken();

    public string GenerateRefreshToken()
    {
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            expires: DateTime.UtcNow.AddSeconds(_jwt.RefreshTokenLifetime),
            signingCredentials: _signingCredentials);

        return _handler.WriteToken(token);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new BadRequestException("Access Token is required");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        };
        var principal = _handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

        if (validatedToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new UnauthorizedException("Invalid token security algorithm.");

        return principal;
    }
}