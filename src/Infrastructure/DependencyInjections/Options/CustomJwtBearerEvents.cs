namespace Infrastructure;

public sealed class CustomJwtBearerEvents : JwtBearerEvents
{
    public override Task MessageReceived(MessageReceivedContext context)
    {
        // Cookie fallback khi client không gửi header Authorization
        if (context.Request.Cookies.TryGetValue("access_token", out var cookie))
            context.Token = cookie;

        return Task.CompletedTask;
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail("Access token is missing or invalid.", StatusCodes.Status401Unauthorized));
    }

    public override Task Forbidden(ForbiddenContext context)
    {
        context.Response.StatusCode  = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(ExceptionMessages.Forbidden, StatusCodes.Status403Forbidden));
    }
}
