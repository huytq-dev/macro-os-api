namespace API;

[RegisterService(typeof(ICurrentUser))]
public sealed class CurrentUser(IHttpContextAccessor _httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid UserId => Principal?.GetUserId() ?? throw new UnauthorizedException("Không xác định được danh tính người dùng.");
    public string Email => Principal?.GetUserEmail() ?? string.Empty;
    public string Role => Principal?.GetUserRole() ?? string.Empty;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
