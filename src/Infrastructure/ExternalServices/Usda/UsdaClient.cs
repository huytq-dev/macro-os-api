using System.Text.Json;

namespace Infrastructure;

/// <summary>HttpClient định kiểu tới USDA FoodData Central v1. Khoá API gửi qua header X-Api-Key (không nằm trong URL nên không lọt vào log).</summary>
public sealed class UsdaClient(HttpClient _http, IOptions<UsdaOptions> _options, ILogger<UsdaClient> _logger) : IUsdaClient
{
    public async Task<IReadOnlyList<int>> SearchAsync(string query, int pageSize, CancellationToken ct)
    {
        var url = $"foods/search?query={Uri.EscapeDataString(query)}&dataType=Foundation,SR%20Legacy&pageSize={pageSize}";
        using var doc = await GetJsonAsync(url, ct);
        return UsdaFoodParser.ParseSearchIds(doc.RootElement);
    }

    public async Task<IReadOnlyList<UsdaFood>> GetFoodsAsync(IReadOnlyList<int> fdcIds, CancellationToken ct)
    {
        if (fdcIds.Count == 0) return [];
        using var doc = await GetJsonAsync($"foods?fdcIds={string.Join(',', fdcIds)}&format=full", ct);
        return UsdaFoodParser.ParseMany(doc.RootElement);
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _options.Value.ApiKey);
            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "USDA request failed: {Path}", url.Split('?')[0]);
            throw new ExternalServiceException("Không gọi được USDA FoodData Central lúc này (có thể hết hạn mức DEMO_KEY), thử lại sau.");
        }
    }
}
