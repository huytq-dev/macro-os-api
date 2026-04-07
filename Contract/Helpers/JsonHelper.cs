using System.Text.Json;

namespace Contract;

public static class JsonHelper
{
    public static string? Serialize<T>(T? obj)
    {
        return obj is null ? string.Empty : JsonSerializer.Serialize(obj);
    }

    public static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind != JsonValueKind.Undefined;
        }
        catch
        {
            return false;
        }
    }
}
