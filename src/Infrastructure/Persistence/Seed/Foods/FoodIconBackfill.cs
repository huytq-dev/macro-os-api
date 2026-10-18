namespace Infrastructure;

/// <summary>
/// Gán icon tự động cho món hệ thống/USDA/OFF/VN đang chưa có IconKey (món seed cũ, món import trước khi có resolver). Chạy lúc khởi động sau các seed, idempotent, theo lô.
/// Món user tự tạo không đụng (user tự chọn hoặc để trống) và món QuickAdd là món ẩn. Món resolver không chắc vẫn null nên mỗi lần khởi động được xét lại (rẻ, chỉ trong bộ nhớ).
/// </summary>
public static class FoodIconBackfill
{
    private const int BatchSize = 500;

    public static async Task RunAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        // Phân trang bằng Skip: món đã gán rời khỏi tập "IconKey == null", món không gán được ở lại nên cộng vào skip
        var skip = 0;
        while (true)
        {
            var rows = await db.Set<FoodItem>().IgnoreQueryFilters()
                .Where(f => f.IconKey == null && f.Source != FoodSource.UserCreated && f.Source != FoodSource.QuickAdd)
                .OrderBy(f => f.Id)
                .Skip(skip).Take(BatchSize)
                .ToListAsync(ct);
            if (rows.Count == 0) break;

            foreach (var f in rows) f.IconKey = IconKeyResolver.Resolve(f.Name, f.Category);
            await db.SaveChangesAsync(ct);
            skip += rows.Count(f => f.IconKey is null);
        }
    }
}
