namespace Application;

/// <summary>
/// Xếp hạng ứng viên khi tìm rộng (không có canonical). Thay cho "Name.Contains rồi lấy tên ngắn nhất":
/// khớp hẳn (tên hoặc alias) &gt; khớp đầu tên &gt; chứa nguyên từ &gt; chứa chuỗi; hòa thì tránh phần phụ ("bone", "fat"…), rồi tới nguồn.
/// </summary>
public static class FoodMatchRanker
{
    // ponytail: danh sách phần phụ viết tay; thay bằng điểm theo dữ liệu khi có đủ lịch sử xác nhận (AiIngredientCache)
    private static readonly HashSet<string> Qualifiers = new(StringComparer.Ordinal)
    {
        "bone", "bones", "fat", "skin", "separable", "trimmings", "byproducts", "offal", "drippings", "lard", "tallow", "broth", "bouillon", "baby", "imitation",
    };

    /// <summary>0 khớp hẳn tên/alias · 1 đầu tên (trước dấu phẩy) · 2 chứa nguyên từ · 3 chứa chuỗi · 4 còn lại.</summary>
    public static int Tier(string foodName, IEnumerable<string> aliasesNormalized, string queryNormalized)
    {
        var q = queryNormalized;
        var n = FoodNameNormalizer.Normalize(foodName);
        if (q.Length == 0) return 4;
        if (n == q || aliasesNormalized.Any(a => a == q)) return 0;
        var head = FoodNameNormalizer.Normalize(foodName.Split(',')[0]);
        if (head == q) return 1;
        if ((" " + n + " ").Contains(" " + q + " ")) return 2;
        return n.Contains(q) ? 3 : 4;
    }

    /// <summary>Số từ phụ trong tên mà người hỏi không nhắc tới.</summary>
    public static int QualifierPenalty(string foodName, string queryNormalized)
    {
        var asked = queryNormalized.Split(' ').ToHashSet();
        return FoodNameNormalizer.Normalize(foodName).Split(' ').Count(w => Qualifiers.Contains(w) && !asked.Contains(w));
    }

    public static IOrderedEnumerable<T> Rank<T>(IEnumerable<T> items, string queryNormalized,
        Func<T, string> name, Func<T, IEnumerable<string>> aliases, Func<T, int> sourcePriority)
        => items.OrderBy(i => Tier(name(i), aliases(i), queryNormalized))
            .ThenBy(i => QualifierPenalty(name(i), queryNormalized))
            .ThenBy(i => sourcePriority(i))
            .ThenBy(i => name(i).Length)
            .ThenBy(i => name(i), StringComparer.OrdinalIgnoreCase);
}
