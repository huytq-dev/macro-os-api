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
    // Ghi cả nội dung request/response tốn CPU và bộ nhớ đúng lúc hệ thống đang chậm và có thể chứa dữ liệu người dùng,
    // nên mặc định chỉ bật khi chạy Development; đặt Application:LogSlowRequestPayload (true/false) để ghi đè.
    private readonly bool _logPayload = config.GetValue("Application:LogSlowRequestPayload", config["ASPNETCORE_ENVIRONMENT"] == "Development");

    private const int MaxResponseChars = 4000;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(ct);
        sw.Stop();

        if (sw.ElapsedMilliseconds > _threshold)
        {
            // Request nhạy cảm (mật khẩu, token, ảnh) ẩn cả request lẫn response, vì response đăng nhập chứa token
            var hide = !_logPayload || request is ISensitiveRequest;
            logger.LogWarning(
                "Slow request [{Name}] took {Ms}ms {@Request} => {Response}", typeof(TRequest).Name, sw.ElapsedMilliseconds,
                hide ? "(đã ẩn)" : request, hide ? "(đã ẩn)" : Describe(response));
        }

        return response;
    }

    // Response dạng JSON, cắt ở MaxResponseChars để log không phình khi trả danh sách lớn
    private static string Describe(TResponse response)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(response);
            return json.Length <= MaxResponseChars ? json : $"{json[..MaxResponseChars]}… (+{json.Length - MaxResponseChars} ký tự)";
        }
        catch (Exception)
        {
            return $"({typeof(TResponse).Name}, không chuyển được sang JSON)";
        }
    }
}
