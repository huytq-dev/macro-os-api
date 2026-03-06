namespace API;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));

        var userIdString = principal.FindFirstValue("Id")
                           ?? principal.FindFirstValue("id")
                           ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdString))
            throw new UnauthorizedException("Không xác định được danh tính người dùng.");

        return Guid.Parse(userIdString);
    }
}