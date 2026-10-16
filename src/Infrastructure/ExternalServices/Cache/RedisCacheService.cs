using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure;

public class RedisCacheService : IRedisCacheService
{
    private readonly HttpClient _http;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record UpstashResp(JsonElement? Result, string? Error);

    public RedisCacheService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var baseUrl = config["Upstash:BaseUrl"] ?? throw new Exception("Upstash BaseUrl missing");
        var token = config["Upstash:Token"] ?? throw new Exception("Upstash Token missing");

        _http.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task SetRecordAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var ex = (int)(expiry?.TotalSeconds ?? 600);
        var data = value is string s ? s : JsonSerializer.Serialize(value, JsonOpts);

        var resp = await _http.GetAsync($"set/{Uri.EscapeDataString(key)}/{Uri.EscapeDataString(data)}?ex={ex}");
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Redis SET failed: {body}");

        var parsed = JsonSerializer.Deserialize<UpstashResp>(body, JsonOpts);
        if (parsed?.Error is not null)
            throw new Exception($"Redis SET error: {parsed.Error}");
    }

    public async Task<T?> GetRecordAsync<T>(string key)
    {
        var resp = await _http.GetAsync($"get/{Uri.EscapeDataString(key)}");
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Redis GET failed: {body}");

        var parsed = JsonSerializer.Deserialize<UpstashResp>(body, JsonOpts);
        if (parsed?.Error is not null)
            throw new Exception($"Redis GET error: {parsed.Error}");

        if (parsed?.Result is null || parsed.Result.Value.ValueKind == JsonValueKind.Null)
            return default;

        if (typeof(T) == typeof(string))
            return (T)(object)parsed.Result.Value.GetString()!;

        return JsonSerializer.Deserialize<T>(parsed.Result.Value.GetRawText(), JsonOpts);
    }

    public async Task RemoveRecordAsync(string key)
    {
        var resp = await _http.GetAsync($"del/{Uri.EscapeDataString(key)}");
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Redis DEL failed: {await resp.Content.ReadAsStringAsync()}");
    }

    public async Task RemoveManyAsync(params string[] keys)
    {
        if (keys.Length == 0) return;

        var joinedKeys = string.Join("/", keys.Select(Uri.EscapeDataString));
        var resp = await _http.GetAsync($"del/{joinedKeys}");

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Redis DEL MANY failed: {await resp.Content.ReadAsStringAsync()}");
    }
}