namespace Application;

/// <summary>Yêu cầu chứa dữ liệu nhạy cảm (mật khẩu, token, email): không được ghi nội dung vào nhật ký, kể cả khi chạy chậm.</summary>
public interface ISensitiveRequest;

public sealed class PerformancePipelineBehavior<TRequest, TResponse>(
    ILogger<PerformancePipelineBehavior<TRequest, TResponse>> logger,
    IConfiguration config)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly long _threshold = config.GetValue("Application:PerformanceThresholdMs", 500L);
    // Ghi cả nội dung request tốn CPU và bộ nhớ đúng lúc hệ thống đang chậm, nên chỉ bật khi cần (Development).
    private readonly bool _logPayload = config.GetValue("Application:LogSlowRequestPayload", false);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(ct);
        sw.Stop();

        if (sw.ElapsedMilliseconds > _threshold)
            logger.LogWarning(
                "Slow request [{Name}] took {Ms}ms {@Request}", typeof(TRequest).Name, sw.ElapsedMilliseconds,
                !_logPayload || request is ISensitiveRequest ? "(đã ẩn)" : request);

        return response;
    }
}
