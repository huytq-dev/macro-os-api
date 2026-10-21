using System.Text.RegularExpressions;

namespace Application;

/// <summary>
/// Đối chiếu món catalog với món USDA FoodData Central (thuần, test offline): dựng từ khoá tìm, chấm độ khớp tên, so macro và chất phụ.
/// Khớp theo từ khoá của TÊN (không theo macro) để một món sai không tự được "xác minh"; macro chỉ dùng làm lưới an toàn ở <see cref="IsSuspicious"/>.
/// </summary>
public static class UsdaCatalogMatcher
{
    /// <summary>Lệch quá ngưỡng này (tương đối) mới báo.</summary>
    public const decimal DiffThreshold = 0.05m;

    /// <summary>Khớp tên tốt nhưng kcal lệch hơn mức này thì coi là khớp nhầm, cần người xem (không tự ghi).</summary>
    public const decimal SuspiciousKcalPercent = 0.25m;

    /// <summary>Protein/carbs/fat lệch hơn mức này (tương đối, và một bên ≥ 2 g) cũng coi là khớp nhầm.</summary>
    public const decimal SuspiciousMacroPercent = 0.5m;

    private const decimal MatchCoverage = 0.75m, MatchScore = 0.7m, WeakCoverage = 0.4m;
    private const decimal MinKcalDiff = 3m, MinGramDiff = 0.3m;

    private static readonly HashSet<string> Stop = new(StringComparer.Ordinal) { "and", "or", "with", "without", "the", "of", "in", "a", "an", "made", "from" };

    // Từ chỉ hình thức/cách chế biến: vẫn tính vào độ phủ (raw ≠ cooked) nhưng một mình không đủ để coi là cùng loại thực phẩm
    private static readonly HashSet<string> Generic = new(StringComparer.Ordinal)
    {
        "raw", "cooked", "fresh", "dry", "dried", "ground", "powder", "whole", "plain", "boiled", "fried", "roasted", "grilled", "steamed", "baked", "canned",
    };

    // Từ thừa ở mô tả FDC (mà tên món mình không có) đổi bản chất món: "Oil, almond" không phải "Almonds", "Tofu yogurt" không phải "Tofu", "Sweet potato" không phải "Potato"…
    private static readonly HashSet<string> NatureWords = new(StringComparer.Ordinal)
    {
        "oil", "juice", "flour", "starch", "bread", "babyfood", "imitation", "yogurt", "breaded", "battered", "sweet", "glutinous", "brown", "powder", "powdered", "canned", "syrup",
        "dried", "dehydrated", "frozen", "concentrate", "concentrated", "substitute", "artificial", "flavored", "sauce", "paste", "puree", "pie", "cake", "cookie", "cracker", "chip",
        "candy", "drink", "non", "fried", "nonfat", "lowfat", "reduced", "light", "diet", "low", "free", "skim", "sweetened",
    };

    // Danh từ nhóm đứng đầu mô tả SR/Foundation ("Nuts, almonds", "Beverages, almond milk"): được phép đứng trước danh từ chính của món
    private static readonly HashSet<string> CategoryPrefixes = new(StringComparer.Ordinal)
    {
        "nut", "seed", "beverage", "cereal", "fish", "vegetable", "fruit", "snack", "spice", "legume", "dessert", "meat", "poultry", "game", "sausage", "restaurant", "alcoholic", "pasta", "candie", "soup",
        "crustacean", "mollusk", "leavening", "agent",
    };

    // Tên khác nhau giữa catalog và FDC: một từ của tên mình coi như CÓ MẶT khi mọi từ của một phương án đều có trong mô tả FDC (kể cả phần trong ngoặc, vd "(tamari)");
    // các từ của phương án đó cũng được phép xuất hiện ở mô tả (không bị coi là từ thừa/bổ ngữ lạ). Không nới các luật chống khớp sai: phương án phải có ĐỦ từ.
    private static readonly Dictionary<string, string[][]> Synonyms = new(StringComparer.Ordinal)
    {
        ["ghee"] = [["butter", "oil"]],                       // Butter oil, anhydrous
        ["pomelo"] = [["pummelo"]],                           // Pummelo, raw
        ["mint"] = [["spearmint"], ["peppermint"]],           // Spearmint, fresh
        ["lemongrass"] = [["lemon", "grass"]],                // Lemon grass (citronella), raw
        ["scallion"] = [["onion", "spring"]],                 // Onions, spring or scallions, …
        ["cumin"] = [["cumin", "seed"]],                      // Spices, cumin seed
        ["tamari"] = [["soy", "sauce"]],                      // Soy sauce made from soy (tamari)
        ["ground"] = [["spice"]],                             // "Cumin, Ground" ↔ "Spices, cumin seed" (SR gọi gia vị xay là nhóm Spices)
    };

    private static List<List<string>> Segments(string name)
        => Regex.Replace(name, @"\([^)]*\)", " ").Split(',').Select(s => Tokens(s).ToList()).Where(s => s.Count > 0).ToList();

    /// <summary>Từ khoá chuẩn hoá của một tên (bỏ phần trong ngoặc: tên Việt/ghi chú), số ít, bỏ từ nối.</summary>
    public static IReadOnlyList<string> Tokens(string name) => RawTokens(Regex.Replace(name, @"\([^)]*\)", " "));

    // Như Tokens nhưng giữ cả chữ trong ngoặc của mô tả FDC ("(tamari)", "(citronella)") để tính độ phủ
    private static IReadOnlyList<string> RawTokens(string text)
        => FoodNameNormalizer.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !Stop.Contains(t)).Select(Singular).Distinct().ToList();

    // Số ít đơn giản: potatoes → potato, berries → berry, almonds → almond; giữ nguyên glutinous/hummus/asparagus/swiss (đuôi us/is/ss)
    private static string Singular(string t)
    {
        if (t.Length <= 3 || !t.EndsWith('s') || t.EndsWith("ss") || t.EndsWith("us") || t.EndsWith("is")) return t;
        if (t.EndsWith("oes")) return t[..^2];
        if (t.EndsWith("ies") && t.Length > 4) return t[..^3] + "y";
        return t[..^1];
    }

    /// <summary>Chuỗi gửi cho FDC search: tên bỏ ngoặc, dấu phẩy → khoảng trắng.</summary>
    public static string Query(string name)
        => string.Join(' ', Regex.Replace(name, @"\([^)]*\)", " ").Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));

    // Từ trong tên catalog nhưng mô tả USDA thường không ghi ("Milk, whole…" vốn là sữa không đường): không bắt buộc phải có ở mô tả FDC
    private static readonly HashSet<string> Ignorable = new(StringComparer.Ordinal) { "unsweetened", "plain", "regular" };

    public static UsdaCandidate Score(string ourName, UsdaFood food)
    {
        var all = Tokens(ourName);
        var ours = all.Where(t => !Ignorable.Contains(t)).ToList();
        if (ours.Count == 0) ours = all.ToList();
        var theirs = RawTokens(food.Description).ToHashSet();
        if (ours.Count == 0 || theirs.Count == 0) return new UsdaCandidate(food, 0, 0, 0);
        bool Has(string t) => theirs.Contains(t) || (Synonyms.TryGetValue(t, out var alternatives) && alternatives.Any(a => a.All(theirs.Contains)));
        var hit = ours.Count(Has);
        var coverageOurs = (decimal)hit / ours.Count;
        var coverageUsda = (decimal)hit / theirs.Count;
        var informative = ours.Where(t => !Generic.Contains(t)).ToList();
        var informativeHit = informative.Count == 0 || informative.Any(Has);
        // Từ của các phương án đồng nghĩa đã thoả được phép xuất hiện ở mô tả
        var allowed = all.SelectMany(t => Synonyms.TryGetValue(t, out var alternatives) ? alternatives.Where(a => a.All(theirs.Contains)).SelectMany(a => a) : []).ToHashSet();
        return new UsdaCandidate(food, Math.Round(coverageOurs, 4), Math.Round(coverageUsda, 4), Math.Round(0.7m * coverageOurs + 0.3m * coverageUsda, 4), informativeHit, FindMismatch(ourName, all, allowed, food));
    }

    /// <summary>
    /// Vì sao ứng viên KHÔNG phải cùng món dù trùng nhiều từ (null = không thấy lý do loại): (1) món thương hiệu/trẻ em/công thức y tế/vùng miền; (2) mô tả có từ đổi bản chất mà tên mình không có
    /// (oil, juice, yogurt, breaded, sweet, brown…); (3) trong cụm danh từ chính (đoạn đầu tiên chứa từ chính của tên mình) có bổ ngữ lạ ("rice milk" ≠ "milk");
    /// (4) đoạn mở đầu mô tả không phải từ của tên mình hay danh từ nhóm quen thuộc ("Oil, almond", "Bread, …").
    /// </summary>
    private static string? FindMismatch(string ourName, IReadOnlyList<string> ours, IReadOnlySet<string> allowed, UsdaFood food)
    {
        if (UsdaCommonFoods.Reject(food) is { } unsuitable) return $"unsuitable:{unsuitable}";
        var own = ours.Concat(allowed).ToHashSet();
        var segments = Segments(food.Description);
        if (segments.SelectMany(s => s).FirstOrDefault(t => NatureWords.Contains(t) && !own.Contains(t)) is { } extra) return $"extra-word:{extra}";

        var ourSegments = Segments(ourName);
        if (ourSegments.Count == 0) return null;
        var head = ourSegments[0].Concat(allowed).ToHashSet();
        var headSegment = segments.FirstOrDefault(s => s.Any(head.Contains));
        if (headSegment is null) return null; // không có từ chính: độ phủ sẽ cho weak/none
        if (headSegment.FirstOrDefault(t => !own.Contains(t) && !Generic.Contains(t)) is { } modifier) return $"modifier:{modifier}";
        var lead = segments[0];
        if (!ReferenceEquals(lead, headSegment) && lead.FirstOrDefault(t => !own.Contains(t) && !Generic.Contains(t) && !CategoryPrefixes.Contains(t)) is { } lead1) return $"lead:{lead1}";
        return null;
    }

    public static UsdaMatchGrade Grade(decimal coverageOurs, decimal score, bool informativeHit = true)
        => !informativeHit ? UsdaMatchGrade.None
            : coverageOurs >= MatchCoverage && score >= MatchScore ? UsdaMatchGrade.Match : coverageOurs >= WeakCoverage ? UsdaMatchGrade.Weak : UsdaMatchGrade.None;

    /// <summary>
    /// Ứng viên tốt nhất: không bị loại (<see cref="UsdaCandidate.Mismatch"/>) trước, điểm cao nhất, hoà điểm thì dataset ưu tiên (Foundation &gt; SR Legacy &gt; FNDDS),
    /// rồi mô tả ngắn hơn, rồi fdcId nhỏ hơn (ổn định giữa các lần chạy); null nếu không có ứng viên.
    /// </summary>
    public static UsdaCandidate? Pick(string ourName, IEnumerable<UsdaFood> candidates) => Rank(ourName, candidates).FirstOrDefault();

    /// <summary>Mọi ứng viên theo đúng thứ tự của <see cref="Pick"/> (để in 3 ứng viên tốt nhất cho người duyệt).</summary>
    public static List<UsdaCandidate> Rank(string ourName, IEnumerable<UsdaFood> candidates)
        => candidates.Select(f => Score(ourName, f)).OrderBy(c => c.Mismatch is null ? 0 : 1).ThenByDescending(c => c.Score)
            .ThenBy(c => UsdaCommonFoods.DatasetRank(c.Food.DataType)).ThenBy(c => c.Food.Description.Length).ThenBy(c => c.Food.FdcId).ToList();

    /// <summary>Kcal: 208, thiếu thì Atwater 957/958 (cùng quy tắc với <see cref="UsdaFoodMapper"/>); protein 203, carbs 205, fat 204 (g).</summary>
    public static UsdaMacros Macros(UsdaFood u)
    {
        var nutrients = UsdaFoodMapper.Canonical(u.Nutrients);
        decimal? Get(string number, string unit)
            => nutrients.FirstOrDefault(n => n.Number == number && n.Unit.Equals(unit, StringComparison.OrdinalIgnoreCase)) is { } v ? v.Amount : null;
        return new UsdaMacros(Get("208", "kcal") ?? Get("957", "kcal") ?? Get("958", "kcal"), Get("203", "g"), Get("205", "g"), Get("204", "g"));
    }

    /// <summary>Macro lệch quá <see cref="DiffThreshold"/> (và lệch tuyệt đối đủ lớn: ≥ 3 kcal, ≥ 0.3 g); chất USDA không có số liệu thì bỏ qua.</summary>
    public static List<UsdaDiff> MacroDiffs(decimal kcal, decimal protein, decimal carbs, decimal fat, UsdaMacros usda)
    {
        var list = new List<UsdaDiff>();
        void Add(string field, decimal ours, decimal? theirs, decimal minAbs)
        {
            if (theirs is not { } t) return;
            if (Diff(field, ours, t, minAbs) is { } d) list.Add(d);
        }
        Add("kcal", kcal, usda.Kcal, MinKcalDiff);
        Add("protein", protein, usda.Protein, MinGramDiff);
        Add("carbs", carbs, usda.Carbs, MinGramDiff);
        Add("fat", fat, usda.Fat, MinGramDiff);
        return list;
    }

    /// <summary>Chất phụ (sodium, fiber…) mình đang có so với USDA cùng đơn vị (µg = mcg); chỉ chất cả hai bên đều có số liệu.</summary>
    public static List<UsdaDiff> NutrientDiffs(IEnumerable<(string Code, string? UsdaNumber, string Unit, decimal Ours)> ours, UsdaFood usda)
    {
        var list = new List<UsdaDiff>();
        foreach (var (code, number, unit, amount) in ours)
        {
            if (number is null) continue;
            var match = usda.Nutrients.FirstOrDefault(n => n.Number == number && NormUnit(n.Unit) == NormUnit(unit));
            if (match is null) continue;
            if (Diff(code, amount, match.Amount, unit == "mg" || unit == "mcg" ? 1m : MinGramDiff) is { } d) list.Add(d);
        }
        return list;
    }

    /// <summary>
    /// Khớp tên tốt nhưng số liệu lệch bất thường so với USDA: kcal lệch &gt; <see cref="SuspiciousKcalPercent"/> (và ≥ 3 kcal), hoặc protein/carbs/fat lệch &gt; <see cref="SuspiciousMacroPercent"/>
    /// khi một bên ≥ 2 g → nghi khớp nhầm món, cần người xem (không tự ghi).
    /// </summary>
    public static bool IsSuspicious(decimal kcal, decimal protein, decimal carbs, decimal fat, UsdaMacros usda)
    {
        if (usda.Kcal is { } k && Math.Abs(kcal - k) >= MinKcalDiff && Math.Abs(kcal - k) / Math.Max(k, 1m) > SuspiciousKcalPercent) return true;
        static bool Off(decimal ours, decimal? theirs)
            => theirs is { } t && Math.Max(ours, t) >= 2m && Math.Abs(ours - t) / Math.Max(t, 0.0001m) > SuspiciousMacroPercent;
        return Off(protein, usda.Protein) || Off(carbs, usda.Carbs) || Off(fat, usda.Fat);
    }

    private static UsdaDiff? Diff(string field, decimal ours, decimal usda, decimal minAbs)
    {
        var abs = Math.Abs(ours - usda);
        if (abs < minAbs) return null;
        var percent = usda == 0m ? 1m : abs / Math.Abs(usda);
        return percent > DiffThreshold ? new UsdaDiff(field, ours, usda, Math.Round(percent, 4)) : null;
    }

    private static string NormUnit(string unit)
    {
        var u = unit.Trim().ToLowerInvariant();
        return u is "µg" or "μg" or "ug" ? "mcg" : u;
    }
}
