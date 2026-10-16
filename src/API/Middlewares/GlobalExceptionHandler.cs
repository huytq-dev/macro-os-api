namespace API;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // Client đã ngắt kết nối: không còn ai nhận phản hồi, không ghi lỗi.
            httpContext.Response.StatusCode = 499;
            return true;
        }

        if (exception is AppException or ValidationException)
            logger.LogWarning("Client error: {Message}", exception.Message);
        else
            logger.LogError(exception, "Unhandled exception");

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validation.Errors
                    .GroupBy(e => e.PropertyName ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) as IDictionary<string, string[]>),
            AppException app => (app.StatusCode, app.Message, app.Errors),
            _                => (StatusCodes.Status500InternalServerError, ExceptionMessages.InternalError, null)
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, statusCode, errors), cancellationToken);
        return true;
    }
}
