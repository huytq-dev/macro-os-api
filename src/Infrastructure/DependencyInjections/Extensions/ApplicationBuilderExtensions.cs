namespace Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // Seed:IncludeVietnamese = false → không seed tên/alias tiếng Việt (danh mục chỉ tiếng Anh); xem FoodCatalogReseeder để dọn dữ liệu đã seed trước đó
        CanonicalCatalogData.IncludeVietnamese = app.Configuration.GetValue("Seed:IncludeVietnamese", true);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Database:AutoMigrate (mặc định true = như trước). false: không tự áp, còn migration chưa áp thì dừng khởi động (production áp S29 có giám sát, xem MigrationStartup)
        await MigrationStartup.ApplyAsync(db, app.Configuration.GetValue("Database:AutoMigrate", true), app.Logger);

        // Danh mục thực phẩm là dữ liệu tham chiếu: seed cả production, idempotent.
        await FoodData.SeedAsync(db);
        await NutrientData.SeedAsync(db);
        await CanonicalCatalogData.SeedAsync(db); // sau FoodData: gom món seed + món Việt vào thực phẩm chuẩn, seed alias
        await FoodIconBackfill.RunAsync(db);      // cuối cùng: gán icon cho món chưa có (seed cũ, import trước đây)

        // Làm nóng chỉ mục tìm món trong RAM (danh mục hệ thống ~12k dòng, ~0.4 s) để người dùng đầu tiên không phải trả phí nạp; lỗi ở đây không được chặn khởi động (request đầu sẽ tự nạp)
        try { await FoodCatalogIndexCache.GetAsync(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), CancellationToken.None); }
        catch (Exception ex) { app.Logger.LogWarning(ex, "Food search index warm-up failed; it will load on first search."); }
    }
}
