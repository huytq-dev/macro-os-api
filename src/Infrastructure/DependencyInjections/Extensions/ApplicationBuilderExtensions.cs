namespace Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // Seed:IncludeVietnamese = false → không seed tên/alias tiếng Việt (danh mục chỉ tiếng Anh); xem FoodCatalogReseeder để dọn dữ liệu đã seed trước đó
        CanonicalCatalogData.IncludeVietnamese = app.Configuration.GetValue("Seed:IncludeVietnamese", true);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        app.Logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Migrations applied.");

        // Danh mục thực phẩm là dữ liệu tham chiếu: seed cả production, idempotent.
        await FoodData.SeedAsync(db);
        await NutrientData.SeedAsync(db);
        await CanonicalCatalogData.SeedAsync(db); // sau FoodData: gom món seed + món Việt vào thực phẩm chuẩn, seed alias
        await FoodIconBackfill.RunAsync(db);      // cuối cùng: gán icon cho món chưa có (seed cũ, import trước đây)
    }
}
