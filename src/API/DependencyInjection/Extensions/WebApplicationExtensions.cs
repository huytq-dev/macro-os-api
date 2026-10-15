namespace API;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiMiddlewares(this WebApplication app)
    {
        app.UseCors("AllowFrontend");
        app.UseHttpsRedirection();
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var response = statusCodeContext.HttpContext.Response;
            if (response.ContentType is not null && response.ContentType.Contains("json")) return;

            response.ContentType = "application/json";
            var message = response.StatusCode switch
            {
                StatusCodes.Status404NotFound         => "API route not found.",
                StatusCodes.Status405MethodNotAllowed => "HTTP method not allowed.",
                StatusCodes.Status401Unauthorized     => "Access token is missing or invalid.",
                StatusCodes.Status403Forbidden        => ExceptionMessages.Forbidden,
                _                                     => "An error occurred while processing the request."
            };
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, response.StatusCode));
        });
        app.UseExceptionHandler();
        app.UseIpRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
