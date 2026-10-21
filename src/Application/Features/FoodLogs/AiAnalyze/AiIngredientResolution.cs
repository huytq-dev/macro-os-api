namespace Application;

/// <summary>Quy tắc chọn biến thể + khẩu phần đếm được cho pha resolve (hàm thuần, kiểm bằng AlgorithmChecks).</summary>
public static class AiIngredientResolution
{
    public const string SourceCache = "cache", SourceCanonical = "canonical", SourceVariant = "variant", SourceSearch = "search", SourceAi = "ai", SourceNone = "none";

    /// <summary>Sai lệch tối đa giữa gram AI ước và Quantity × gram khẩu phần; vượt thì tin khẩu phần trong DB.</summary>
    public const decimal ServingTolerance = 0.4m;

    private static readonly HashSet<string> SizeUnits = new(StringComparer.Ordinal) { "small", "medium", "large", "extra large" };

    // Khẩu phần "cỡ" (trứng/chuối/táo…): chỉ chọn khi AI nêu đơn vị cỡ; khi chọn theo gram thì bỏ qua để 150 g trứng ra "3 piece" chứ không ra "2.4 jumbo"
    private static readonly HashSet<string> SizeLabels = new(StringComparer.OrdinalIgnoreCase) { "extra small", "small", "medium", "large", "extra large", "jumbo" };

    /// <summary>Các khẩu phần dùng để chọn theo gram: bỏ khẩu phần cỡ (trừ khi món chỉ có khẩu phần cỡ).</summary>
    public static List<(Guid Id, string Name, decimal Grams)> ForGramChoice(IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings)
    {
        var plain = servings.Where(s => !SizeLabels.Contains(ServingLabel(s.Name))).ToList();
        return plain.Count > 0 ? plain : servings.ToList();
    }

    /// <summary>Đơn vị đếm AI được phép trả (khớp enum của tool report_meal).</summary>
    public static readonly IReadOnlySet<string> Units = new HashSet<string>(StringComparer.Ordinal)
    {
        "small", "medium", "large", "extra large", "slice", "piece", "cup", "tbsp", "tsp", "bowl", "plate",
    };

    /// <summary>Chữ thường, "_"/"-" → khoảng trắng; không thuộc <see cref="Units"/> → null.</summary>
    public static string? NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;
        var u = unit.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        return Units.Contains(u) ? u : null;
    }

    /// <summary>Khoá bộ nhớ đệm: tên chuẩn hoá, có state thì thêm hậu tố "|state" (state none = khoá cũ).</summary>
    public static string CacheKey(string normalized, string state)
        => state == FoodPreparationState.None ? normalized : $"{normalized}|{state}";

    /// <summary>Bỏ tiền tố "1 " của tên khẩu phần để FE ghép "3 large" ("1 large" → "large"); các tên khác giữ nguyên.</summary>
    public static string ServingLabel(string name)
        => name.StartsWith("1 ", StringComparison.Ordinal) ? name[2..].Trim() : name;

    /// <summary>
    /// Khẩu phần có tên đúng đơn vị (sau khi bỏ "1 "); không có thì khẩu phần chứa nguyên từ đơn vị (ngắn nhất). Đơn vị cỡ (small/medium/large/extra large) chỉ khớp đúng tên
    /// để "large" không rơi vào "extra large". Không khớp → null (rơi về chọn theo gram).
    /// </summary>
    public static (Guid Id, string Name, decimal Grams)? MatchServing(IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings, string unit)
    {
        var usable = servings.Where(s => s.Grams > 0).Select(s => ((Guid Id, string Name, decimal Grams)?)s).ToList();
        var exact = usable.FirstOrDefault(s => ServingLabel(s!.Value.Name).Equals(unit, StringComparison.OrdinalIgnoreCase));
        if (exact is not null || SizeUnits.Contains(unit)) return exact;
        return usable.Where(s => $" {ServingLabel(s!.Value.Name).ToLowerInvariant()} ".Contains($" {unit} "))
            .OrderBy(s => s!.Value.Name.Length).FirstOrDefault();
    }

    /// <summary>Khẩu phần trả cho FE (chip đổi cỡ): bỏ gram ≤ 0, nhãn bỏ tiền tố "1 ", mặc định đứng đầu rồi theo gram tăng dần.</summary>
    public static List<AiServingModel> ServingOptions(IEnumerable<(Guid Id, string Name, decimal Weight, bool IsDefault)> servings)
        => servings.Where(s => s.Weight > 0).OrderByDescending(s => s.IsDefault).ThenBy(s => s.Weight).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => new AiServingModel(s.Id, ServingLabel(s.Name), s.Weight, s.IsDefault)).ToList();

    public sealed record UnitServing(Guid ServingId, string Label, decimal Quantity, decimal WeightGrams, bool Corrected);

    /// <summary>
    /// Chọn khẩu phần theo đơn vị đếm. Số lượng = <paramref name="quantity"/> (thiếu thì suy từ gram, chỉ nhận 0.25–4 khẩu phần); gram = số lượng × gram khẩu phần.
    /// <see cref="UnitServing.Corrected"/> = lệch quá <see cref="ServingTolerance"/> so với gram AI ước (khi đó tin khẩu phần DB, nơi gọi ghi log).
    /// </summary>
    public static UnitServing? ChooseByUnit(IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings, string unit, decimal? quantity, decimal grams)
    {
        if (MatchServing(servings, unit) is not { } s) return null;
        if (quantity is null)
        {
            var ratio = grams / s.Grams;
            if (ratio is < 0.25m or > 4m) return null;
        }
        var qty = quantity ?? NutritionRounding.Round(grams / s.Grams, 2);
        if (qty <= 0) return null;
        var weight = NutritionRounding.Round(qty * s.Grams, 1);
        var corrected = quantity is not null && grams > 0 && Math.Abs(weight - grams) / grams > ServingTolerance;
        return new UnitServing(s.Id, ServingLabel(s.Name), qty, weight, corrected);
    }

    /// <summary>
    /// Chọn món trong các món cùng thực phẩm chuẩn: (1) tên món trùng hẳn tên hỏi (vd "Olive Oil") (2) món có cùng state (vd fried) (3) dòng đại diện.
    /// Trả null khi không có món nào dùng được. Source = canonical (dòng đại diện) hoặc variant (biến thể khác).
    /// </summary>
    public static (T Item, string Source)? PickFood<T>(IReadOnlyList<T> items, Func<T, string> name, Func<T, bool> isRep, IReadOnlyCollection<string> keys, string state)
        where T : class
    {
        if (items.Count == 0) return null;
        var picked = items.Where(i => keys.Contains(FoodNameNormalizer.Normalize(name(i)))).OrderByDescending(isRep).FirstOrDefault();
        if (picked is null && state != FoodPreparationState.None)
            picked = items.Where(i => FoodPreparationState.Detect(name(i)) == state).OrderByDescending(isRep).ThenBy(i => name(i).Length).FirstOrDefault();
        picked ??= items.FirstOrDefault(isRep);
        return picked is null ? null : (picked, isRep(picked) ? SourceCanonical : SourceVariant);
    }
}
