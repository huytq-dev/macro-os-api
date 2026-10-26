using System.Globalization;
using System.Text;

namespace Application;

/// <summary>
/// Ảnh chụp trong bộ nhớ của danh mục thực phẩm HỆ THỐNG (mọi món không phải của user và không phải Quick Add: seed, USDA, Open Food Facts…) cùng alias, dùng cho tìm món theo từng phím gõ.
/// Tên/thương hiệu đã được gấp sẵn (chữ thường, bỏ dấu, đ → d) nên so khớp "pho" ↔ "Phở", "dau hu" ↔ "Đậu hũ" là so chuỗi thường trong RAM: không quét bảng bằng collation (LIKE '%x%' không dùng được index),
/// không kéo hàng nghìn dòng qua mạng, và không tốn một vòng đi về DB cho mỗi nhóm kết quả. Mỗi dòng mang luôn cột hiển thị (macro, khẩu phần mặc định, icon) nên trang kết quả không cần truy vấn chi tiết.
/// Món của user (Custom) và lịch sử log đọc từ DB theo từng request (nhỏ, theo user).
/// </summary>
public sealed class FoodCatalogIndex
{
    public sealed record Entry(
        Guid Id, string Name, string? Brand, string NameFolded, string BrandFolded, FoodSource Source, string? ProviderCode, bool IsRepresentative, bool IsVerified, Guid? CanonicalFoodId,
        FoodCategory Category, string? IconKey, decimal Calories, decimal Protein, decimal Carbs, decimal Fat, Guid? ServingId, string ServingName, decimal ServingGrams, Guid? CreatedByUserId = null)
    {
        public bool HasBrand => !string.IsNullOrEmpty(Brand);
    }

    public sealed record AliasEntry(string Normalized, Guid FoodItemId, Guid? CanonicalFoodId);

    private readonly Dictionary<Guid, Entry> _byId;

    public FoodCatalogIndex(IReadOnlyList<Entry> entries, IReadOnlyList<AliasEntry> aliases)
    {
        Entries = entries; Aliases = aliases;
        _byId = entries.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<Entry> Entries { get; }
    public IReadOnlyList<AliasEntry> Aliases { get; }

    /// <summary>Món hệ thống theo id; null nếu không thuộc chỉ mục (món của user, Quick Add, đã xoá).</summary>
    public Entry? Find(Guid id) => _byId.GetValueOrDefault(id);

    /// <summary>Gấp tên để so khớp: chữ thường, bỏ dấu (đ → d), giữ nguyên dấu phẩy/khoảng trắng (tên kiểu "Salt, table" cần phân biệt "trước dấu phẩy").</summary>
    public static string Fold(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var d = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }

    /// <summary>Dựng Entry từ dữ liệu một FoodItem (cả món hệ thống lẫn món của user, để dùng chung một đường so khớp).</summary>
    public static Entry ToEntry(Guid id, string name, string? brand, FoodSource source, string? providerCode, bool isRepresentative, bool isVerified, Guid? canonicalFoodId, FoodCategory category, string? iconKey,
        decimal calories, decimal protein, decimal carbs, decimal fat, Guid? servingId, string? servingName, decimal? servingGrams, Guid? createdByUserId)
        => new(id, name, brand, Fold(name), Fold(brand), source, providerCode, isRepresentative, isVerified, canonicalFoodId, category, iconKey, calories, protein, carbs, fat,
            servingId, servingName ?? "100 g", servingGrams ?? 100m, createdByUserId);

    public static async Task<FoodCatalogIndex> LoadAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var rows = await uow.Repository<FoodItem>().Query()
            .Where(f => f.Source != FoodSource.QuickAdd && f.Source != FoodSource.UserCreated)
            .Select(f => new
            {
                f.Id, f.Name, f.Brand, f.Source, f.ProviderCode, f.IsRepresentative, f.IsVerified, f.CanonicalFoodId, f.Category, f.IconKey,
                f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
                Serving = f.Servings.OrderByDescending(s => s.IsDefault).Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).FirstOrDefault(),
            })
            .ToListAsync(ct);
        var aliases = await uow.Repository<FoodAlias>().Query().Select(a => new AliasEntry(a.AliasNormalized, a.FoodItemId, a.CanonicalFoodId)).ToListAsync(ct);
        return new FoodCatalogIndex(
            rows.Select(r => ToEntry(r.Id, r.Name, r.Brand, r.Source, r.ProviderCode, r.IsRepresentative, r.IsVerified, r.CanonicalFoodId, r.Category, r.IconKey,
                r.CaloriesPer100g, r.ProteinPer100g, r.CarbsPer100g, r.FatPer100g, r.Serving?.Id, r.Serving?.ServingName, r.Serving?.WeightInGrams, null)).ToArray(),
            aliases);
    }
}

/// <summary>
/// Giữ <see cref="FoodCatalogIndex"/> dùng chung cho cả tiến trình: nạp lười lần đầu (một truy vấn ~12k dòng), làm mới sau <see cref="Ttl"/> và khi danh mục hệ thống đổi
/// (<see cref="Invalidate"/> sau khi import USDA hoặc lưu món Open Food Facts mới từ mã vạch). Nhiều request cùng lúc khi chưa có/đã hết hạn chỉ nạp MỘT lần. Nhiều instance: mỗi instance tự làm mới, trễ tối đa bằng TTL.
/// </summary>
public static class FoodCatalogIndexCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    /// <summary>Trần số món cho chỉ mục trong RAM (~350 byte/món); danh mục lớn hơn nữa cần cột đã gấp + index ở DB (xem tài liệu tối ưu tìm món), không nạp tiếp để khỏi ngốn bộ nhớ.</summary>
    public const int MaxEntries = 300_000;

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static volatile Snapshot? _current;

    private sealed record Snapshot(FoodCatalogIndex Index, long ExpiresAtTicks);

    public static async Task<FoodCatalogIndex> GetAsync(IUnitOfWork uow, CancellationToken ct)
    {
        if (_current is { } c && c.ExpiresAtTicks > Environment.TickCount64) return c.Index;
        await Gate.WaitAsync(ct);
        try
        {
            if (_current is { } again && again.ExpiresAtTicks > Environment.TickCount64) return again.Index;
            var index = await FoodCatalogIndex.LoadAsync(uow, ct);
            if (index.Entries.Count > MaxEntries) throw new InvalidOperationException($"Danh mục {index.Entries.Count} món vượt trần chỉ mục RAM ({MaxEntries}); cần chỉ mục ở DB.");
            _current = new Snapshot(index, Environment.TickCount64 + (long)Ttl.TotalMilliseconds);
            return index;
        }
        finally { Gate.Release(); }
    }

    /// <summary>Danh mục hệ thống vừa đổi: lần tìm kế tiếp nạp lại.</summary>
    public static void Invalidate() => _current = null;
}
