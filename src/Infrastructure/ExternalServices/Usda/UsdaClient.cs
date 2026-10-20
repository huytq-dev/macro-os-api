using System.Text;
using System.Text.Json;

namespace Infrastructure;

/// <summary>HttpClient định kiểu tới USDA FoodData Central v1. Khoá API gửi qua header X-Api-Key (không nằm trong URL nên không lọt vào log).</summary>
public sealed class UsdaClient(HttpClient _http, IOptions<UsdaOptions> _options, ILogger<UsdaClient> _logger) : IUsdaClient
{
    /// <summary>Khoá của <see cref="ExternalServiceException.Errors"/> chứa mã HTTP FDC trả về (lỗi mạng/timeout thì không có khoá này).</summary>
    public const string UpstreamStatusKey = "upstreamStatus";

    // POST /foods/search với JSON: GET mang "Survey (FNDDS)" trong query string bị gateway api.data.gov chặn (400 HTML "nginx") vì ngoặc thô, còn mã hoá %28/%29 thì FDC không nhận
    public async Task<IReadOnlyList<int>> SearchAsync(string query, int pageSize, CancellationToken ct, string dataTypes = UsdaDataTypes.Reference)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        using var doc = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "foods/search")
        {
            Content = new StringContent(BuildSearchBody(query, pageSize, dataTypes), Encoding.UTF8, "application/json"),
        }, "foods/search", ct);
        return UsdaFoodParser.ParseSearchIds(doc.RootElement);
    }

    /// <summary>Thân JSON của POST /foods/search: <c>{"query":…,"dataType":["Foundation","SR Legacy"],"pageSize":n}</c>; dataTypes phân cách bằng dấu phẩy.</summary>
    public static string BuildSearchBody(string query, int pageSize, string dataTypes)
        => JsonSerializer.Serialize(new
        {
            query = query.Trim(),
            dataType = dataTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            pageSize,
        });

    public async Task<IReadOnlyList<UsdaFood>> GetFoodsAsync(IReadOnlyList<int> fdcIds, CancellationToken ct)
    {
        if (fdcIds.Count == 0) return [];
        var url = $"foods?fdcIds={string.Join(',', fdcIds)}&format=full";
        using var doc = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, url), "foods", ct);
        return UsdaFoodParser.ParseMany(doc.RootElement);
    }

    private async Task<JsonDocument> SendAsync(Func<HttpRequestMessage> build, string path, CancellationToken ct)
    {
        try
        {
            using var request = build();
            request.Headers.Add("X-Api-Key", _options.Value.ApiKey);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Ghi thân lỗi của FDC (nhỏ, không chứa khoá) để biết LÝ DO thay vì chỉ "400 Bad Request"
                var body = await response.Content.ReadAsStringAsync(ct);
                var snippet = body.Length > 300 ? body[..300] : body;
                var status = (int)response.StatusCode;
                _logger.LogWarning("USDA request failed: {Path} → {Status} {Body}", path, status, snippet);
                throw new ExternalServiceException(
                    $"USDA FoodData Central trả HTTP {status}{(status == 429 ? " (hết hạn mức, thử lại sau)" : "")}: {snippet}",
                    new Dictionary<string, string[]> { [UpstreamStatusKey] = [status.ToString()] });
            }
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "USDA request failed: {Path}", path);
            throw new ExternalServiceException("Không gọi được USDA FoodData Central lúc này (mạng, quá thời gian hoặc hết hạn mức DEMO_KEY), thử lại sau.");
        }
    }
}
