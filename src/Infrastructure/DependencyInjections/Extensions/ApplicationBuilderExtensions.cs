namespace Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        app.Logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Migrations applied.");

        // Danh mục thực phẩm là dữ liệu tham chiếu: seed cả production, idempotent.
        await FoodData.SeedAsync(db);

        if (app.Environment.IsDevelopment())
        {
            await AppData.SeedAsync(db);
            app.Logger.LogInformation("Development data seeded.");
        }
    }
}
