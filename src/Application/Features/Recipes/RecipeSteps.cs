using System.Text.Json;

namespace Application;

/// <summary>Các bước làm lưu dạng JSON một cột trên Recipe (luôn đọc/ghi cả danh sách, không truy vấn từng bước).</summary>
internal static class RecipeSteps
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Stored(int Order, string Text);

    /// <summary>Sắp theo Order (thiếu thì theo vị trí trong mảng), bỏ bước rỗng, đánh số lại 1..n; không còn bước nào → null.</summary>
    public static string? Serialize(IReadOnlyList<RecipeStepInput>? steps)
    {
        if (steps is null) return null;
        var stored = steps
            .Select((s, i) => (Key: s.Order ?? i + 1, Index: i, Text: s.Text.Trim()))
            .Where(s => s.Text.Length > 0)
            .OrderBy(s => s.Key).ThenBy(s => s.Index)
            .Select((s, i) => new Stored(i + 1, s.Text))
            .ToList();
        return stored.Count == 0 ? null : JsonSerializer.Serialize(stored, Json);
    }

    public static List<RecipeStepModel> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return (JsonSerializer.Deserialize<List<Stored>>(json, Json) ?? []).Select(s => new RecipeStepModel(s.Order, s.Text)).ToList(); }
        catch (JsonException) { return []; }
    }
}
