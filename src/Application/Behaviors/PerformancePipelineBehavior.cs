namespace Application;

public sealed class PerformancePipelineBehavior<TRequest, TResponse>(
    ILogger<PerformancePipelineBehavior<TRequest, TResponse>> logger,
    IConfiguration config)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly long _threshold = config.GetValue("Application:PerformanceThresholdMs", 500L);
    // Ghi nội dung request tốn CPU và bộ nhớ đúng lúc hệ thống đang chậm và có thể chứa dữ liệu người dùng,
    // nên mặc định chỉ bật khi chạy Development; đặt Application:LogSlowRequestPayload (true/false) để ghi đè.
    private readonly bool _logPayload = config.GetValue("Application:LogSlowRequestPayload", config["ASPNETCORE_ENVIRONMENT"] == "Development");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(ct);
        sw.Stop();

        if (sw.ElapsedMilliseconds > _threshold)
        {
            // Chỉ Command (POST/PUT/PATCH/DELETE) mới ghi input để xem API chậm nhận gì; Query (GET) không ghi cả input lẫn response
            // (danh sách lớn làm log phình); Command nhạy cảm (mật khẩu, token, ảnh) cũng ẩn
            var name = typeof(TRequest).Name;
            if (!_logPayload || name.EndsWith("Query", StringComparison.Ordinal) || request is ISensitiveRequest)
                logger.LogWarning("Slow request [{Name}] took {Ms}ms", name, sw.ElapsedMilliseconds);
            else
                logger.LogWarning("Slow request [{Name}] took {Ms}ms {@Request}", name, sw.ElapsedMilliseconds, request);
        }

        return response;
    }
}
