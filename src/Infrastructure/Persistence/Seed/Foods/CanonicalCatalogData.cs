namespace Infrastructure;

/// <summary>
/// Seed Canonical Food Catalog: thực phẩm chuẩn + dòng đại diện + nguồn + alias Việt/Anh. Idempotent:
/// 60 món seed (FoodData) và bộ món Việt (<see cref="VietnameseFoods"/>) mỗi cái có một canonical; alias gắn vào canonical (và dòng đại diện).
/// Chạy sau FoodData. Một alias (đã chuẩn hoá) chỉ thuộc một canonical; trùng thì bỏ alias sau (check kiểm bắt lỗi khi soạn dữ liệu).
/// </summary>
public static partial class CanonicalCatalogData
{
    /// <summary>
    /// false = KHÔNG seed tên/alias tiếng Việt (chỉ tiếng Anh): bỏ alias có dấu và dạng không dấu của chúng, canonical không có NameVi.
    /// Đặt lúc khởi động từ cấu hình <c>Seed:IncludeVietnamese</c>; mặc định true (giữ hành vi cũ khi chưa cấu hình).
    /// </summary>
    public static bool IncludeVietnamese { get; set; } = true;

    /// <summary>Chuẩn hoá của mọi alias/tên CHỈ-Việt trong dữ liệu seed (tập alias khi bật tiếng Việt trừ tập alias tiếng Anh); dùng để dọn alias Việt đã lưu trong DB.</summary>
    public static HashSet<string> VietnameseAliasNorms()
    {
        var saved = IncludeVietnamese;
        try
        {
            IncludeVietnamese = true;
            var all = Entries(FoodData.VietnameseNames).SelectMany(e => e.Aliases).Select(FoodNameNormalizer.Normalize).ToHashSet();
            IncludeVietnamese = false;
            all.ExceptWith(Entries(FoodData.VietnameseNames).SelectMany(e => e.Aliases).Select(FoodNameNormalizer.Normalize));
            // Tên Anh liệt kê tường minh (kể cả tên món Việt viết không dấu như "Pho", "Com tam") luôn được giữ
            all.ExceptWith(FoodData.EnglishAliasMap.Values.SelectMany(v => v.Split('|')).Select(FoodNameNormalizer.Normalize));
            all.ExceptWith(VietnameseFoods.Select(v => FoodNameNormalizer.Normalize(v.En)));
            all.Remove("");
            return all;
        }
        finally { IncludeVietnamese = saved; }
    }

    private static IEnumerable<string> Filter(IEnumerable<string> tokens, IEnumerable<string?> knownVietnamese, IEnumerable<string>? explicitEnglish = null)
        => IncludeVietnamese ? tokens : FoodEnglish.EnglishOnly(tokens, knownVietnamese.Where(v => !string.IsNullOrEmpty(v))!, explicitEnglish);

    /// <summary>Danh sách (key, tên Anh chuẩn, tên Việt, alias) cho mọi canonical của seed; tách riêng để test kiểm trùng alias. seedNames = tên Việt của món seed theo key.</summary>
    public static IEnumerable<(string Key, string En, string? Vi, string[] Aliases)> Entries(IReadOnlyDictionary<string, string> seedNames)
    {
        foreach (var (key, en) in FoodData.EnglishAliasMap)
        {
            var english = en.Split('|');
            var vi = SeedVietnameseAliases.GetValueOrDefault(key)?.Split('|') ?? [];
            var name = seedNames.GetValueOrDefault(key);
            var tokens = english.Concat(vi).Append(name ?? "").Concat(AttachedAliases(key)).Where(a => a.Length > 0);
            // Tên Việt không dấu trùng một tên tiếng Anh đã liệt kê (Pizza, Hamburger…) là tên Anh hợp lệ, không bị lọc
            var englishNorms = english.Select(FoodNameNormalizer.Normalize).ToHashSet();
            yield return (key, english[0], IncludeVietnamese ? name : null, Filter(tokens, vi.Append(name).Where(x => x is not null && !englishNorms.Contains(FoodNameNormalizer.Normalize(x))), english).ToArray());
        }
        foreach (var v in VietnameseFoods)
            yield return (v.Key, v.En, IncludeVietnamese ? v.Vi : null,
                Filter(v.Aliases.Split('|').Append(v.En).Append(v.Vi).Concat(AttachedAliases(v.Key)), [v.Vi], [v.En]).ToArray());

        // Nhóm gia vị: canonical mới (gom biến thể theo tên Anh); biến thể của canonical đã có đã được gộp alias ở trên (AttachedAliases)
        foreach (var group in CatalogVariants.GroupBy(c => c.Canonical).Where(g => !ExistingCanonicalKeys.ContainsKey(g.Key)))
        {
            var rep = group.FirstOrDefault(c => c.Rep) ?? group.First();
            var tokens = group.SelectMany(c => c.Aliases.Split('|').Append(c.Name).Append(c.Vi ?? "")).Append(group.Key).Where(a => a.Length > 0);
            yield return ($"cond:{FoodNameNormalizer.Normalize(group.Key)}", group.Key, IncludeVietnamese ? rep.Vi : null, Filter(tokens, group.Select(c => c.Vi), group.Select(c => c.Name).Append(group.Key)).ToArray());
        }
    }

    /// <summary>Tên + alias của các biến thể gia vị gắn vào canonical đã có (key món seed của canonical đó).</summary>
    private static IEnumerable<string> AttachedAliases(string existingKey)
        => CatalogVariants.Where(c => ExistingCanonicalKeys.TryGetValue(c.Canonical, out var k) && k == existingKey)
            .SelectMany(c => Filter(c.Aliases.Split('|').Append(c.Name).Append(c.Vi ?? ""), [c.Vi], [c.Name]))
            .Where(a => a.Length > 0);
}
