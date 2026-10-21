namespace Application;

/// <summary>Khớp tên nguyên liệu AI với món trong DB: tách từ và xếp hạng mức khớp. Chọn khẩu phần và tính macro do <see cref="NutritionCalculator"/> lo.</summary>
public static class AiIngredientMatcher
{
    /// <summary>Từ khoá tìm kiếm: chữ thường, bỏ dấu câu, số ít, tối đa 4 từ.</summary>
    public static IReadOnlyList<string> Words(string name)
        => Tokens(name).Select(Singular).Distinct().Take(4).ToList();

    // ponytail: bỏ "s" cuối cho tên tiếng Anh (Eggs → Egg); từ bất quy tắc (leaves, potatoes) để USDA alias lo
    private static string Singular(string w) => w.Length > 3 && w.EndsWith('s') && !w.EndsWith("ss") ? w[..^1] : w;

    private static IEnumerable<string> Tokens(string s)
        => new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>0 trùng hẳn · 1 bắt đầu bằng · 2 chứa nguyên từ · 3 chứa chuỗi · 4 chỉ khớp theo từng từ/alias.</summary>
    public static int Tier(string foodName, string term)
    {
        var n = foodName.ToLowerInvariant();
        var t = string.Join(' ', Words(term));
        if (n == t) return 0;
        if (n.StartsWith(t)) return 1;
        if ((" " + string.Join(' ', Tokens(n)) + " ").Contains(" " + t + " ")) return 2;
        return n.Contains(t) ? 3 : 4;
    }

    /// <summary>Thứ tự ưu tiên nguồn: hệ thống/USDA đã xác minh → hệ thống/USDA → của user → Open Food Facts.</summary>
    public static int SourcePriority(FoodSource source, bool verified, bool mine) => source switch
    {
        FoodSource.System or FoodSource.USDA => verified ? 0 : 1,
        FoodSource.UserCreated when mine => 2,
        FoodSource.OpenFoodFacts => 3,
        _ => 4,
    };
}
