using System.Globalization;
using System.Text.RegularExpressions;

namespace Application;

/// <summary>
/// Hiển thị khẩu phần và suy khối lượng riêng (g/ml) từ khẩu phần thể tích của món, để FE bật thêm đơn vị tbsp/tsp/fl oz/cup/ml.
/// Mô tả USDA dạng "&lt;modifier&gt; - &lt;kích cỡ&gt;" (vd "large - 3 1/4\" diameter") giữ nguyên, chỉ bỏ "1 " đầu.
/// </summary>
public static partial class FoodServingText
{
    /// <summary>"1 large - 3 1/4\" diameter" → "large - 3 1/4\" diameter"; chỉ bỏ số 1 đứng riêng ở đầu ("100 g", "1.5 cup" giữ nguyên).</summary>
    public static string Label(string servingName)
    {
        var name = servingName.Trim();
        var label = LeadingOne().Replace(name, "");
        return label.Length > 0 ? label : name;
    }

    // ml của một đơn vị thể tích (US)
    private static decimal MlPer(string unit) => unit.ToLowerInvariant() switch
    {
        var u when u.StartsWith("cup") => 236.588m,
        var u when u.StartsWith("tbsp") || u.StartsWith("tablespoon") => 14.7868m,
        var u when u.StartsWith("tsp") || u.StartsWith("teaspoon") => 4.92892m,
        var u when u.StartsWith("fl") || u.StartsWith("fluid") => 29.5735m,
        var u when u.StartsWith("ml") || u.StartsWith("milli") => 1m,
        _ => 1000m, // liter
    };

    /// <summary>
    /// g/ml của món: giá trị lưu trên món nếu có; không thì suy từ khẩu phần thể tích (cup, tbsp, tsp, fl oz, ml, l; vd "1 cup, sliced" = 109 g → 0.4607).
    /// Ưu tiên khẩu phần trơn (không mô tả thêm), rồi khẩu phần mặc định, rồi theo tên. Ngoài 0.05–3.5 g/ml coi là dữ liệu hỏng, trả null.
    /// </summary>
    public static decimal? GramsPerMl(FoodItem food)
    {
        if (food.DensityGPerMl is > 0) return food.DensityGPerMl;

        var best = food.Servings
            .Select(s => (Serving: s, Match: Volume().Match(s.ServingName)))
            .Where(x => x.Match.Success && x.Serving.WeightInGrams > 0)
            .Select(x => (x.Serving, Extra: x.Match.Groups["rest"].Value.Trim(), Ml: Amount(x.Match.Groups["amt"].Value) * MlPer(x.Match.Groups["unit"].Value)))
            .Where(x => x.Ml > 0)
            .OrderBy(x => x.Extra.Length == 0 ? 0 : 1).ThenByDescending(x => x.Serving.IsDefault).ThenBy(x => x.Serving.ServingName, StringComparer.Ordinal)
            .Select(x => x.Serving.WeightInGrams / x.Ml)
            .Where(d => d is >= 0.05m and <= 3.5m)
            .Cast<decimal?>().FirstOrDefault();
        return best is { } v ? Math.Round(v, 4) : null;
    }

    // "", "1", "0.5", ".5", "1/2", "1 1/2"
    private static decimal Amount(string text)
    {
        text = text.Trim();
        if (text.Length == 0) return 1m;
        decimal total = 0;
        foreach (var part in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Contains('/'))
            {
                var f = part.Split('/');
                if (!decimal.TryParse(f[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var n)
                    || !decimal.TryParse(f[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var d) || d == 0) return 0;
                total += n / d;
            }
            else if (decimal.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) total += v;
            else return 0;
        }
        return total;
    }

    [GeneratedRegex(@"^1\s+(?=\S)")]
    private static partial Regex LeadingOne();

    [GeneratedRegex(@"^\s*(?<amt>\d+\s+\d+/\d+|\d+/\d+|\d*\.?\d+)?\s*(?<unit>cups?|tbsp|tablespoons?|tsp|teaspoons?|fl\.?\s?oz|fluid\s+ounces?|ml|milliliters?|liters?|litres?)\b(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex Volume();
}
