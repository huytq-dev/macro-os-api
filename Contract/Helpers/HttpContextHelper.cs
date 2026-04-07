namespace Contract;

public static class HttpContextHelper
{
    public static Guid GetCurrentUserId(this HttpContext httpContext)
    {
        var currentId = httpContext?.User.FindFirstValue("Id");
        Guid.TryParse(currentId, out var userId);
        return userId;
    }
}
