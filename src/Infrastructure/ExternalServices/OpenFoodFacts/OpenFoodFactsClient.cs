using System.Net;
using System.Text.Json;

namespace Infrastructure;

/// <summary>HttpClient định kiểu tới Open Food Facts API v2 (BaseAddress, timeout, User-Agent đặt ở ServiceCollectionExtensions).</summary>
public sealed class OpenFoodFactsClient(HttpClient _http, ILogger<OpenFoodFactsClient> _logger) : IOpenFoodFactsClient
{
    private const string Fields = "code,product_name,product_name_vi,generic_name,brands,serving_quantity,serving_size,image_front_url,nutriments";

    public async Task<OffProduct?> FindByBarcodeAsync(string code, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync($"api/v2/product/{code}?fields={Fields}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return OpenFoodFactsParser.Parse(code, doc.RootElement);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // client ngắt kết nối, không phải lỗi OFF
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Open Food Facts lookup failed for {Code}", code);
            throw new ExternalServiceException("Không tra được Open Food Facts lúc này, thử lại sau.");
        }
    }
}
