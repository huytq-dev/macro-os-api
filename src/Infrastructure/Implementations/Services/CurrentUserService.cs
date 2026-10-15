using System.IdentityModel.Tokens.Jwt;

namespace Infrastructure;

[RegisterService(typeof(IUserContext))]
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string Role  => Principal?.FindFirstValue(ClaimTypes.Role)  ?? string.Empty;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
