namespace Application;

/// <summary>
/// Cách chế biến của một món (raw, boiled, fried…). KHÔNG lưu cột riêng: suy ra từ tên món (tiếng Anh và tiếng Việt, vd "Egg, hard-boiled", "trứng chiên")
/// nên không cần đổi schema; AI cũng trả cùng bộ giá trị ở trường <c>state</c> của nguyên liệu.
/// </summary>
public static class FoodPreparationState
{
    public const string None = "none";

    /// <summary>Bộ giá trị AI/FE được phép gửi (state không rõ = <see cref="None"/>).</summary>
    public static readonly IReadOnlySet<string> Values = new HashSet<string>(StringComparer.Ordinal)
    {
        "raw", "boiled", "fried", "scrambled", "grilled", "steamed", "baked", "roasted", "stir_fried", None,
    };

    // Thứ tự quan trọng: cụm dài/đặc hiệu trước ("stir fried" trước "fried"). Token đã chuẩn hoá (chữ thường, không dấu).
    private static readonly (string State, string[] Words)[] Words =
    [
        ("stir_fried", ["stir fried", "stir fry", "xao"]),
        ("scrambled", ["scrambled", "scramble"]),
        ("fried", ["fried", "fry", "chien", "ran", "op la"]),
        ("poached", ["poached"]),
        ("boiled", ["boiled", "luoc"]),
        ("steamed", ["steamed", "hap"]),
        ("baked", ["baked"]),
        ("grilled", ["grilled", "nuong"]),
        ("roasted", ["roasted", "rang", "quay"]),
        ("raw", ["raw", "song"]),
    ];

    /// <summary>true nếu <paramref name="state"/> null/trống hoặc thuộc <see cref="Values"/> (không phân biệt hoa thường, "stir-fried" = "stir_fried").</summary>
    public static bool IsValid(string? state) => string.IsNullOrWhiteSpace(state) || Values.Contains(Canonical(state));

    /// <summary>Dạng chuẩn của state AI/FE gửi; null, trống hoặc không hợp lệ → <see cref="None"/>.</summary>
    public static string Normalize(string? state)
        => string.IsNullOrWhiteSpace(state) || !Values.Contains(Canonical(state)) ? None : Canonical(state);

    private static string Canonical(string state) => state.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');

    /// <summary>State suy từ tên món (raw, boiled, poached, fried…); không có từ chỉ cách chế biến → <see cref="None"/>. "poached" chỉ có ở danh mục, AI không gửi.</summary>
    public static string Detect(string? name)
    {
        var padded = $" {FoodNameNormalizer.Normalize(name)} ";
        foreach (var (state, words) in Words)
            if (words.Any(w => padded.Contains($" {w} "))) return state;
        return None;
    }
}
