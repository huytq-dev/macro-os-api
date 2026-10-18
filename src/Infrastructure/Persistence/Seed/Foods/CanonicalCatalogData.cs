namespace Infrastructure;

/// <summary>
/// Seed Canonical Food Catalog: thực phẩm chuẩn + dòng đại diện + nguồn + alias Việt/Anh. Idempotent:
/// 60 món seed (FoodData) và bộ món Việt (<see cref="VietnameseFoods"/>) mỗi cái có một canonical; alias gắn vào canonical (và dòng đại diện).
/// Chạy sau FoodData. Một alias (đã chuẩn hoá) chỉ thuộc một canonical; trùng thì bỏ alias sau (check kiểm bắt lỗi khi soạn dữ liệu).
/// </summary>
public static partial class CanonicalCatalogData
{
    /// <summary>Danh sách (key, tên Anh chuẩn, tên Việt, alias) cho mọi canonical của seed; tách riêng để test kiểm trùng alias. seedNames = tên Việt của món seed theo key.</summary>
    public static IEnumerable<(string Key, string En, string? Vi, string[] Aliases)> Entries(IReadOnlyDictionary<string, string> seedNames)
    {
        foreach (var (key, en) in FoodData.EnglishAliasMap)
        {
            var english = en.Split('|');
            var vi = SeedVietnameseAliases.GetValueOrDefault(key)?.Split('|') ?? [];
            var name = seedNames.GetValueOrDefault(key);
            yield return (key, english[0], name, english.Concat(vi).Append(name ?? "").Concat(AttachedAliases(key)).Where(a => a.Length > 0).ToArray());
        }
        foreach (var v in VietnameseFoods)
            yield return (v.Key, v.En, v.Vi, v.Aliases.Split('|').Append(v.En).Append(v.Vi).Concat(AttachedAliases(v.Key)).ToArray());

        // Nhóm gia vị: canonical mới (gom biến thể theo tên Anh); biến thể của canonical đã có đã được gộp alias ở trên (AttachedAliases)
        foreach (var group in Condiments.GroupBy(c => c.Canonical).Where(g => !ExistingCanonicalKeys.ContainsKey(g.Key)))
        {
            var rep = group.FirstOrDefault(c => c.Rep) ?? group.First();
            var aliases = group.SelectMany(c => c.Aliases.Split('|').Append(c.Name).Append(c.Vi ?? "")).Append(group.Key).Where(a => a.Length > 0).ToArray();
            yield return ($"cond:{FoodNameNormalizer.Normalize(group.Key)}", group.Key, rep.Vi, aliases);
        }
    }

    /// <summary>Tên + alias của các biến thể gia vị gắn vào canonical đã có (key món seed của canonical đó).</summary>
    private static IEnumerable<string> AttachedAliases(string existingKey)
        => Condiments.Where(c => ExistingCanonicalKeys.TryGetValue(c.Canonical, out var k) && k == existingKey)
            .SelectMany(c => c.Aliases.Split('|').Append(c.Name).Append(c.Vi ?? ""))
            .Where(a => a.Length > 0);
}
