using System.Text.RegularExpressions;

namespace Application;

/// <summary>
/// Lớp "common foods" kiểu MacroFactor từ file tải về của FDC (Foundation, SR Legacy, Survey FNDDS; KHÔNG Branded): bỏ món thiếu kcal/macro, đồ cho trẻ sơ sinh, công thức y tế, món vùng miền/commodity,
/// món có thương hiệu (heuristic), món trùng tên với catalog hoặc với món USDA ưu tiên cao hơn. Hàm thuần, test offline.
/// </summary>
public static class UsdaCommonFoods
{
    private const int SampleLimit = 8;

    // Nhóm (SR "Baby Foods", FNDDS "Baby food: …") chứa "baby"/"infant"; trong TÊN chỉ bắt cụm rõ nghĩa để không loại nhầm "Spinach, baby", "Carrots, baby", "Baby Ruth" (kẹo, đã bị lọc thương hiệu)
    private static readonly string[] BabyCategoryWords = ["baby", "infant", "human milk"];
    private static readonly string[] BabyNameWords = ["baby food", "babyfood", "infant", "toddler", "human milk"];
    private static readonly string[] RegionalWords =
        ["american indian", "alaska native", "navajo", "hopi", "apache", "shoshone", "pima", "northern plains", "southwest", "ojibwa", "inuit", "usda commodity", "commodity"];
    private static readonly string[] MedicalWords =
        ["enteral", "medical food", "nutritional supplement", "ensure", "pediasure", "glucerna", "slimfast", "slim fast", "instant breakfast", "formulated bar", "boost"];
    // Thương hiệu hay gặp trong SR Legacy/FNDDS (tên dạng chuẩn hoá); ngoài danh sách này còn bắt từ VIẾT HOA toàn bộ (xem IsBranded)
    private static readonly string[] BrandWords =
    [
        "pillsbury", "kraft", "pepperidge farm", "nabisco", "kellogg", "kellogg s", "quaker", "campbell", "campbell s", "heinz", "nestle", "hershey", "hershey s", "mcdonald", "wendy", "burger king", "taco bell",
        "pizza hut", "little caesars", "olive garden", "george weston", "interstate brands", "sage valley", "mary s gone", "la ricura", "general mills", "ocean spray", "knorr", "lipton", "oscar mayer",
        "tyson", "hormel", "smucker", "jimmy dean", "stouffer", "lean cuisine", "healthy choice", "banquet", "hungry man", "marie callender", "digiorno", "boston market", "subway", "applebee",
        "arby", "chick fil", "dunkin", "starbucks", "kfc", "popeyes", "domino", "papa john", "ihop", "denny", "cracker barrel", "on the border", "red lobster", "carl s jr", "jack in the box",
        "dairy queen", "nathan s", "kashi", "betty crocker", "duncan hines", "keebler", "post", "rice a roni", "uncle ben", "cheerios", "frito lay", "doritos", "pringles",
    ];
    private static readonly HashSet<string> Acronyms = new(StringComparer.Ordinal) { "USDA", "UPC", "NFS", "IQF", "BBQ", "MSG", "RTE", "USA" };

    /// <summary>0 Foundation (phân tích mới nhất) → 1 SR Legacy → 2 Survey (FNDDS): món trùng tên thì dataset thấp hơn thắng.</summary>
    public static int DatasetRank(string dataType) => dataType switch { "Foundation" => 0, "SR Legacy" => 1, _ => 2 };

    /// <summary>Lý do loại theo nội dung món (null = giữ lại); chưa xét trùng tên.</summary>
    public static UsdaSkipReason? Reject(UsdaFood u)
    {
        var macros = UsdaCatalogMatcher.Macros(u);
        if (macros.Kcal is null) return UsdaSkipReason.NoEnergy;
        if (macros.Protein is null && macros.Carbs is null && macros.Fat is null) return UsdaSkipReason.NoMacros;

        var category = Padded(u.Category);
        var name = Padded(u.Description);
        if (BabyCategoryWords.Any(w => category.Contains($" {w}")) || BabyNameWords.Any(w => name.Contains($" {w} "))) return UsdaSkipReason.BabyOrInfant;
        if (MedicalWords.Any(w => name.Contains($" {w} "))) return UsdaSkipReason.MedicalFormula;
        if (RegionalWords.Any(w => category.Contains($" {w}") || name.Contains($" {w} "))) return UsdaSkipReason.RegionalOrCommodity;
        if (IsBranded(u.Description, name)) return UsdaSkipReason.Branded;
        return null;
    }

    // Từ viết hoa toàn bộ ≥ 3 chữ cái (KRAFT, ALMOND JOY) hoặc cụm thương hiệu quen thuộc; "(Alaska Native)", "(Includes ...)" đã bị bỏ ở nơi khác nên không gây nhầm
    private static bool IsBranded(string description, string paddedName)
    {
        var withoutParens = Regex.Replace(description, @"\([^)]*\)", " ");
        // Sở hữu cách ở ĐOẠN ĐẦU ("Rudi's Organic Bakery, …", "Andrea's, …", "Udi's, …") hầu như luôn là nhãn hàng trong SR/FNDDS; "Mushroom, lion's mane" (đoạn sau) không bị tính
        if (Regex.IsMatch(withoutParens.Split(',')[0], @"[A-Za-z]['’]s\b")) return true;
        foreach (Match m in Regex.Matches(withoutParens, @"[A-Za-z][A-Za-z']*"))
            if (m.Value.Length >= 3 && m.Value.All(char.IsUpper) && !Acronyms.Contains(m.Value)) return true;
        return BrandWords.Any(b => paddedName.Contains($" {b} "));
    }

    private static string Padded(string? s) => $" {FoodNameNormalizer.Normalize(s)} ";

    /// <summary>Tên hiển thị gọn: bỏ phần trong ngoặc ("(Includes foods for USDA's Food Distribution Program)"), UPC, ", NFS", ", NS as to …", ", USDA Commodity…"; giữ nguyên các phần còn lại.</summary>
    public static string CleanName(string description)
    {
        var s = Regex.Replace(description, @"\s*\([^)]*\)", "");
        s = Regex.Replace(s, @",?\s*UPC:?\s*\d+", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @",\s*(NFS|NS as to [^,]*|USDA Commodity[^,]*)(?=,|$)", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\s+", " ").Trim(' ', ',');
        s = Regex.Replace(s, @",\s*,", ",");
        if (s.Length > 256) s = s[..256].TrimEnd(' ', ',');
        return s.Length == 0 ? description.Trim() : s;
    }

    /// <summary>
    /// Chọn món sẽ nhập. <paramref name="catalogKeys"/> = tên chuẩn hoá đã có trong DB (catalog, canonical, món USDA đã nhập); <paramref name="importedFdcIds"/> = fdcId đã nhập.
    /// Thứ tự ưu tiên Foundation → SR Legacy → FNDDS rồi fdcId nhỏ; <paramref name="max"/> cắt SAU khi xếp (phần vượt tính OverLimit).
    /// </summary>
    public static UsdaBulkPlan Plan(IEnumerable<UsdaFood> foods, IReadOnlySet<string> catalogKeys, IReadOnlySet<string> importedFdcIds, int? max = null)
    {
        var skipped = new Dictionary<UsdaSkipReason, int>();
        var samples = new Dictionary<UsdaSkipReason, List<string>>();
        void Skip(UsdaSkipReason reason, string description)
        {
            skipped[reason] = skipped.GetValueOrDefault(reason) + 1;
            if (!samples.TryGetValue(reason, out var list)) samples[reason] = list = [];
            if (list.Count < SampleLimit) list.Add(description);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var selected = new List<UsdaBulkCandidate>();
        foreach (var u in foods.OrderBy(f => DatasetRank(f.DataType)).ThenBy(f => f.FdcId))
        {
            if (importedFdcIds.Contains(u.FdcId.ToString())) { Skip(UsdaSkipReason.AlreadyImported, u.Description); continue; }
            if (Reject(u) is { } reason) { Skip(reason, u.Description); continue; }

            var name = CleanName(u.Description);
            var key = FoodNameNormalizer.Normalize(name);
            if (key.Length == 0) { Skip(UsdaSkipReason.NoMacros, u.Description); continue; }
            if (catalogKeys.Contains(key) || catalogKeys.Contains(FoodNameNormalizer.Normalize(u.Description))) { Skip(UsdaSkipReason.ExistsInCatalog, u.Description); continue; }
            if (!seen.Add(key)) { Skip(UsdaSkipReason.DuplicateName, u.Description); continue; }
            selected.Add(new UsdaBulkCandidate(u, name, key));
        }

        if (max is { } cap && selected.Count > cap)
        {
            foreach (var over in selected.Skip(cap)) Skip(UsdaSkipReason.OverLimit, over.Food.Description);
            selected = selected.Take(cap).ToList();
        }
        return new UsdaBulkPlan(selected, skipped, samples);
    }
}
