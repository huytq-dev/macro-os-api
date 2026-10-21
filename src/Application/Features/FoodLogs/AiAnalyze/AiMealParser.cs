using System.Globalization;
using System.Text.Json;

namespace Application;

/// <summary>Đọc JSON tool-input do model trả (đã ép schema nhưng vẫn phải chịu được thiếu/sai kiểu).</summary>
public static class AiMealParser
{
    public static AiMealDraft ParseMeal(JsonElement root)
    {
        var dishes = new List<AiDraftDish>();
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("dishes", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in arr.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object))
            {
                var ingredients = new List<AiDraftIngredient>();
                if (e.TryGetProperty("ingredients", out var ing) && ing.ValueKind == JsonValueKind.Array)
                    foreach (var i in ing.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.Object))
                        ingredients.Add(new AiDraftIngredient(Str(i, "name") ?? "", Num(i, "grams"), NumOrNull(i, "quantity"), Str(i, "unit"), Str(i, "state")));
                dishes.Add(new AiDraftDish(Str(e, "name") ?? "", ingredients));
            }
        }
        return new AiMealDraft(dishes, root.ValueKind == JsonValueKind.Object ? Str(root, "confidence") : null);
    }

    public static AiIngredientEstimate ParseEstimate(JsonElement root)
        => root.ValueKind == JsonValueKind.Object
            ? new AiIngredientEstimate(Num(root, "calories"), Num(root, "protein_g"), Num(root, "fat_g"), Num(root, "carbs_g"))
            : new AiIngredientEstimate(0, 0, 0, 0);

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? NumOrNull(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.Number or JsonValueKind.String ? Num(e, name) : (decimal?)null;

    // Model đôi khi trả số dạng chuỗi; thiếu/sai → 0
    private static decimal Num(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return 0;
        return v.ValueKind switch
        {
            JsonValueKind.Number when v.TryGetDecimal(out var d) => d,
            JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
            _ => 0,
        };
    }
}
