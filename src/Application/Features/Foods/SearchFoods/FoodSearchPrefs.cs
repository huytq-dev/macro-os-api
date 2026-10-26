namespace Application;

/// <summary>
/// Hai cài đặt Food Log ảnh hưởng tìm món (Show Branded, Show Open Food Facts; chưa lưu = bật cả hai), cache ngắn theo user: tìm món chạy sau MỖI lần gõ phím nên không được tốn một vòng đi về DB chỉ để đọc cài đặt.
/// <see cref="Remember"/> ghi đè cache ngay khi user đổi cài đặt (UpdateFoodLogSettings); hết hạn sau <see cref="Ttl"/> thì đọc lại DB (an toàn khi có nhiều instance, trễ tối đa bằng TTL).
/// </summary>
public static class FoodSearchPrefs
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private static string Key(Guid userId) => $"foodsearch:prefs:{userId:N}";

    public static async Task<(bool Branded, bool OpenFoodFacts)> GetAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
    {
        if (uow.Cache.TryGet<(bool Branded, bool OpenFoodFacts)>(Key(userId), out var cached)) return cached;
        var row = await uow.Repository<FoodLogSetting>().Query().Where(s => s.UserId == userId)
            .Select(s => new { s.ShowBrandedResults, s.ShowOpenFoodFactsResults }).FirstOrDefaultAsync(ct);
        var value = (row?.ShowBrandedResults ?? true, row?.ShowOpenFoodFactsResults ?? true);
        uow.Cache.Set(Key(userId), value, Ttl);
        return value;
    }

    public static void Remember(IUnitOfWork uow, Guid userId, bool branded, bool openFoodFacts) => uow.Cache.Set(Key(userId), (branded, openFoodFacts), Ttl);
}
