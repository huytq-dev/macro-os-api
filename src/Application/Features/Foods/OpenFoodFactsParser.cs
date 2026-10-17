using System.Globalization;
using System.Text.Json;

namespace Application;

/// <summary>
/// Đọc JSON sản phẩm của Open Food Facts API v2 (thuần, không mạng). Nutriments của OFF tính theo gram /100 g:
/// đổi sang mg (sodium, kali, canxi, sắt, vitamin C, cholesterol) và µg (vitamin D) cho khớp <see cref="FoodNutrition"/>.
/// </summary>
public static class OpenFoodFactsParser
{
    /// <summary>null nếu không có sản phẩm hoặc không có dữ liệu năng lượng/macro nào.</summary>
    public static OffProduct? Parse(string code, JsonElement root)
    {
        if (root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Number && status.GetInt32() == 0) return null;
        if (!root.TryGetProperty("product", out var p) || p.ValueKind != JsonValueKind.Object) return null;
        if (!p.TryGetProperty("nutriments", out var n) || n.ValueKind != JsonValueKind.Object) return null;

        decimal? Num(string key) => n.TryGetProperty(key, out var v) && ToDecimal(v) is { } d && d >= 0 ? d : null;
        decimal? Scaled(string key, decimal factor) => Num(key) is { } g ? Math.Round(g * factor, 2) : null;
        decimal? Macro(string key) => Num(key) is { } g ? Math.Min(g, 100m) : null;   // OFF có dữ liệu bẩn; /100 g không thể quá 100

        var protein = Macro("proteins_100g");
        var carbs = Macro("carbohydrates_100g");
        var fat = Macro("fat_100g");
        var kcal = Num("energy-kcal_100g") ?? (Num("energy-kj_100g") is { } kj ? Math.Round(kj / 4.184m, 1) : null);
        if (kcal is null && protein is null && carbs is null && fat is null) return null;
        kcal ??= Math.Round(4 * (protein ?? 0) + 4 * (carbs ?? 0) + 9 * (fat ?? 0), 1);

        var nutrition = new FoodNutrition(
            kcal.Value, protein ?? 0, carbs ?? 0, fat ?? 0,
            FiberPer100g: Macro("fiber_100g"),
            SugarPer100g: Macro("sugars_100g"),
            SodiumPer100g: Scaled("sodium_100g", 1000m),
            PotassiumPer100g: Scaled("potassium_100g", 1000m),
            CalciumPer100g: Scaled("calcium_100g", 1000m),
            IronPer100g: Scaled("iron_100g", 1000m),
            VitaminCPer100g: Scaled("vitamin-c_100g", 1000m),
            VitaminDPer100g: Scaled("vitamin-d_100g", 1_000_000m),
            SaturatedFatPer100g: Macro("saturated-fat_100g"),
            TransFatPer100g: Macro("trans-fat_100g"),
            CholesterolPer100g: Scaled("cholesterol_100g", 1000m));

        var brand = Text(p, "brands")?.Split(',')[0].Trim();
        var name = Text(p, "product_name_vi") ?? Text(p, "product_name") ?? Text(p, "generic_name") ?? (string.IsNullOrEmpty(brand) ? $"Sản phẩm {code}" : brand);

        decimal? servingGrams = p.TryGetProperty("serving_quantity", out var sq) && ToDecimal(sq) is { } q and > 0 and <= 5000 ? q : null;
        var servingName = Text(p, "serving_size");
        if (servingName is { Length: > 100 }) servingName = null;

        return new OffProduct(code, Cut(name, 256), string.IsNullOrEmpty(brand) ? null : Cut(brand, 128), Text(p, "image_front_url"), servingGrams, servingName, nutrition);
    }

    private static decimal? ToDecimal(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Number => v.TryGetDecimal(out var d) ? d : null,
        JsonValueKind.String => decimal.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null,
        _ => null,
    };

    private static string? Text(JsonElement obj, string key)
        => obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()) ? v.GetString()!.Trim() : null;

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..max];
}
