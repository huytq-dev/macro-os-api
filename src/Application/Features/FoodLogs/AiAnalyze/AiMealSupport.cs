using System.Globalization;
using System.Text.Json;

namespace Application;

/// <summary>Nhận loại ảnh theo magic bytes.</summary>
public static class ImageSniffer
{
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string? Detect(ReadOnlySpan<byte> d)
    {
        if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return "image/jpeg";
        if (d.Length >= 8 && d[..8].SequenceEqual(PngSignature)) return "image/png";
        if (d.Length >= 12 && d[..4].SequenceEqual("RIFF"u8) && d[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}

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

/// <summary>Làm sạch kết quả nhận diện trước khi trả FE: tên gọn, gram trong biên, giới hạn số món/nguyên liệu. AI chỉ nhận diện nên không bao giờ tin số thô.</summary>
public static class AiPlateNormalizer
{
    private const int MaxDishes = 10, MaxIngredients = 15, MaxGrams = 5000, MaxQuantity = 100;

    public static AiAnalysisResponse Normalize(AiMealDraft draft)
    {
        var dishes = new List<AiDishModel>();
        foreach (var d in draft.Dishes)
        {
            var name = Clean(d.Name, 100);
            var ingredients = d.Ingredients
                .Select(i => (Name: Clean(i.Name, 100), Grams: NutritionRounding.Round(Math.Clamp(i.Grams, 0, MaxGrams), 0), Draft: i))
                .Where(i => i.Name is not null && i.Grams > 0 && !IsZeroCalorieSeasoning(i.Name))
                .Take(MaxIngredients)
                .Select(i => ToModel(i.Name!, i.Grams, i.Draft))
                .ToList();
            if (name is null || ingredients.Count == 0) continue;

            dishes.Add(new AiDishModel(Guid.NewGuid(), name, ingredients));
            if (dishes.Count == MaxDishes) break;
        }

        var confidence = draft.Confidence?.Trim().ToLowerInvariant();
        return new AiAnalysisResponse(dishes, true, confidence is "low" or "medium" or "high" ? confidence : "medium");
    }

    // Quantity chỉ có nghĩa khi có Unit hợp lệ (đếm được); state không rõ/không hợp lệ → null
    private static AiIngredientModel ToModel(string name, decimal grams, AiDraftIngredient draft)
    {
        var unit = AiIngredientResolution.NormalizeUnit(draft.Unit);
        decimal? quantity = unit is not null && draft.Quantity is > 0 and <= MaxQuantity ? NutritionRounding.Round(draft.Quantity.Value, 2) : null;
        var state = FoodPreparationState.Normalize(draft.State);
        return new AiIngredientModel(Guid.NewGuid(), name, grams, quantity, quantity is null ? null : unit, state == FoodPreparationState.None ? null : state);
    }

    /// <summary>Chặn biên + làm tròn gọn ước lượng một nguyên liệu (kcal 0 số lẻ, macro 1); kcal lệch macro quá ngưỡng thì lấy theo macro (chỉ ở biên AI, xem <see cref="NutritionCalculator.ReconcileKcal"/>).</summary>
    public static AiIngredientEstimate NormalizeEstimate(AiIngredientEstimate e)
    {
        var p = NutritionRounding.Round(Math.Clamp(e.Protein, 0, 1000), NutritionRounding.CompactMacro);
        var f = NutritionRounding.Round(Math.Clamp(e.Fat, 0, 1000), NutritionRounding.CompactMacro);
        var c = NutritionRounding.Round(Math.Clamp(e.Carbs, 0, 1000), NutritionRounding.CompactMacro);
        var kcal = NutritionRounding.Round(Math.Clamp(e.Calories, 0, 5000), NutritionRounding.CompactKcal);
        return new AiIngredientEstimate(NutritionCalculator.ReconcileKcal(kcal, p, c, f), p, f, c);
    }

    // Gia vị ≈0 kcal: prompt đã bảo model bỏ, đây là lưới an toàn khi model vẫn trả. So khớp NGUYÊN tên (chuẩn hoá) nên "Bell Pepper"/"Chili Pepper" không bị chặn.
    private static readonly HashSet<string> ZeroCalorieSeasonings = new(StringComparer.OrdinalIgnoreCase)
    {
        "salt", "sea salt", "table salt", "kosher salt", "pepper", "black pepper", "white pepper", "ground pepper", "ground black pepper",
        "msg", "monosodium glutamate", "seasoning", "seasoning powder", "stock powder", "chicken powder", "bouillon", "bouillon powder", "chicken bouillon",
        "muoi", "tieu", "bot ngot", "hat nem",
    };

    public static bool IsZeroCalorieSeasoning(string name)
        => ZeroCalorieSeasonings.Contains(FoodNameNormalizer.Normalize(name));

    private static string? Clean(string? s, int max)
    {
        s = s?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        return s.Length > max ? s[..max] : s;
    }
}
