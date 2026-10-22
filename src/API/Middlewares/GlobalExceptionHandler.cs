namespace API;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) : IExceptionHandler
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
            logger.LogError(exception, "Unhandled exception (trace {TraceId})", httpContext.TraceIdentifier);

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validation.Errors
                    .GroupBy(e => e.PropertyName ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) as IDictionary<string, string[]>),
            AppException app => (app.StatusCode, app.Message, app.Errors),
            // 500: kèm traceId (khớp dòng log "Unhandled exception") để báo lỗi từ FE tra được nguyên nhân; Development thêm loại + thông điệp exception
            _                => (StatusCodes.Status500InternalServerError, ExceptionMessages.InternalError, InternalErrorDetails(httpContext, exception))
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, statusCode, errors, (exception as AppException)?.Details), cancellationToken);
        return true;
    }

    private IDictionary<string, string[]> InternalErrorDetails(HttpContext httpContext, Exception exception)
    {
        var details = new Dictionary<string, string[]> { ["traceId"] = [httpContext.TraceIdentifier] };
        if (env.IsDevelopment()) details["exception"] = [$"{exception.GetType().Name}: {(exception.InnerException ?? exception).Message}"];
        return details;
    }
}
