namespace API;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        ApiResponse<object> response;

        if (exception is AppException appEx)
        {
            httpContext.Response.StatusCode = appEx.StatusCode;

            response = ApiResponse<object>.Error(
                message: appEx.Message,
                statusCode: appEx.StatusCode,
                errors: appEx.Errors
            );
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            response = ApiResponse<object>.Error(
                message: ExceptionMessages.InternalError,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}