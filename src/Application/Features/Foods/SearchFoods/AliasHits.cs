namespace Application;

/// <summary>
/// Món/thực phẩm chuẩn có alias khớp từ khoá (đã chuẩn hoá, bỏ dấu, khớp theo nguyên từ: gõ "muoi"/"muối"/"salt" đều ra Salt và các biến thể cùng canonical).
/// Tải TRƯỚC bằng một truy vấn nhỏ trên bảng alias rồi lọc món bằng <c>IN (...)</c>: viết thành subquery tương quan (<c>alias.FoodItemId = f.Id OR alias.CanonicalFoodId = f.CanonicalFoodId</c>)
/// thì SQL Server chạy lại nó cho từng dòng FoodItem (không dùng được index vì có OR) cả ở WHERE lẫn ORDER BY, với ~12k món USDA mất vài giây.
/// </summary>
public sealed record AliasHits(IReadOnlyList<Guid> WordFoodIds, IReadOnlyList<Guid> WordCanonicalIds, IReadOnlyList<Guid> ExactFoodIds, IReadOnlyList<Guid> ExactCanonicalIds)
{
    public static readonly AliasHits None = new([], [], [], []);

    /// <param name="norm">Từ khoá đã chuẩn hoá (<see cref="FoodNameNormalizer.Normalize"/>); dưới 2 ký tự không khớp alias.</param>
    public static async Task<AliasHits> LoadAsync(IUnitOfWork uow, string norm, CancellationToken ct)
    {
        if (norm.Length < 2) return None;
        var rows = await uow.Repository<FoodAlias>().Query()
            .Where(a => a.AliasNormalized == norm || a.AliasNormalized.StartsWith(norm + " ") || a.AliasNormalized.EndsWith(" " + norm) || a.AliasNormalized.Contains(" " + norm + " "))
            .Select(a => new { a.FoodItemId, a.CanonicalFoodId, Exact = a.AliasNormalized == norm })
            .ToListAsync(ct);
        if (rows.Count == 0) return None;

        static List<Guid> Distinct(IEnumerable<Guid?> ids) => ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        return new AliasHits(
            Distinct(rows.Select(r => (Guid?)r.FoodItemId)), Distinct(rows.Select(r => r.CanonicalFoodId)),
            Distinct(rows.Where(r => r.Exact).Select(r => (Guid?)r.FoodItemId)), Distinct(rows.Where(r => r.Exact).Select(r => r.CanonicalFoodId)));
    }
}
