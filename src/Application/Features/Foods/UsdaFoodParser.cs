using System.Text.Json;

namespace Application;

/// <summary>Đọc JSON của USDA FoodData Central (thuần, không mạng). Hỗ trợ dạng chi tiết (<c>nutrient.number</c> + <c>amount</c>) và dạng tóm tắt của search (<c>nutrientNumber</c> + <c>value</c>).</summary>
public static class UsdaFoodParser
{
    private const int MaxPortions = 10;

    public static UsdaFood? Parse(JsonElement f)
    {
        if (f.ValueKind != JsonValueKind.Object || !f.TryGetProperty("fdcId", out var id) || !id.TryGetInt32(out var fdcId)) return null;
        var description = Str(f, "description");
        if (string.IsNullOrWhiteSpace(description)) return null;

        var nutrients = new List<UsdaNutrientValue>();
        if (f.TryGetProperty("foodNutrients", out var list) && list.ValueKind == JsonValueKind.Array)
            foreach (var n in list.EnumerateArray())
            {
                string? number, unit;
                decimal? amount;
                if (n.TryGetProperty("nutrient", out var meta) && meta.ValueKind == JsonValueKind.Object)
                {
                    number = Str(meta, "number");
                    unit = Str(meta, "unitName");
                    amount = Num(n, "amount");
                }
                else
                {
                    number = Str(n, "nutrientNumber");
                    unit = Str(n, "unitName");
                    amount = Num(n, "value");
                }
                // amount thiếu = USDA không có số liệu: bỏ, không coi là 0
                if (number is null || unit is null || amount is not { } a || a < 0) continue;
                nutrients.Add(new UsdaNutrientValue(number, unit, a));
            }

        var portions = new List<UsdaPortion>();
        if (f.TryGetProperty("foodPortions", out var plist) && plist.ValueKind == JsonValueKind.Array)
            foreach (var p in plist.EnumerateArray())
            {
                if (Num(p, "gramWeight") is not { } grams || grams <= 0) continue;
                var name = PortionName(p);
                if (name is null || portions.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
                portions.Add(new UsdaPortion(name, Math.Round(grams, 2)));
                if (portions.Count == MaxPortions) break;
            }

        return new UsdaFood(fdcId, description.Trim(), Str(f, "dataType") ?? "", Category(f), portions, nutrients);
    }

    /// <summary>Phần tử không đọc được bị bỏ qua.</summary>
    public static List<UsdaFood> ParseMany(JsonElement root)
        => root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().Select(Parse).OfType<UsdaFood>().ToList() : [];

    /// <summary>fdcId theo thứ tự của kết quả search.</summary>
    public static List<int> ParseSearchIds(JsonElement root)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty("foods", out var foods) && foods.ValueKind == JsonValueKind.Array
            ? foods.EnumerateArray().Where(x => x.TryGetProperty("fdcId", out var i) && i.ValueKind == JsonValueKind.Number)
                .Select(x => x.GetProperty("fdcId").GetInt32()).ToList()
            : [];

    // "1 cup, sliced"; mô tả có sẵn thì dùng, không thì ghép số lượng + đơn vị + cách chế biến
    private static string? PortionName(JsonElement p)
    {
        var desc = Str(p, "portionDescription");
        if (!string.IsNullOrWhiteSpace(desc) && !desc.StartsWith("Quantity not specified", StringComparison.OrdinalIgnoreCase)) return Cut(desc);

        var unit = p.TryGetProperty("measureUnit", out var mu) && mu.ValueKind == JsonValueKind.Object ? Str(mu, "name") : null;
        if (unit is "undetermined") unit = null;
        var modifier = Str(p, "modifier");
        if (string.IsNullOrWhiteSpace(unit) && string.IsNullOrWhiteSpace(modifier)) return null;

        var amount = Num(p, "amount") ?? 1m;
        var name = $"{amount:0.##} {unit}".Trim();
        if (!string.IsNullOrWhiteSpace(modifier)) name = unit is null ? $"{amount:0.##} {modifier}" : $"{name}, {modifier}";
        return Cut(name);
    }

    private static string Cut(string s) => s.Length <= 100 ? s : s[..100];

    private static string? Category(JsonElement f)
    {
        if (!f.TryGetProperty("foodCategory", out var c)) return null;
        return c.ValueKind switch
        {
            JsonValueKind.Object => Str(c, "description"),
            JsonValueKind.String => c.GetString(),
            _ => null,
        };
    }

    private static string? Str(JsonElement e, string key)
        => e.TryGetProperty(key, out var v) ? v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null,
        } : null;

    private static decimal? Num(JsonElement e, string key)
        => e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d) ? d : null;
}
