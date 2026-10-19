using Microsoft.Data.SqlClient;

namespace Infrastructure;

/// <summary>
/// Lệnh bảo trì chạy từ dòng lệnh thay vì mở web: <c>dotnet run --project src/API -- reseed-foods [--apply --backed-up]</c>. CHỈ chạy ở Development.
/// Chạy TRƯỚC khi áp migration/seed của lần khởi động thường (Program.cs gọi trước ApplyMigrationsAsync) nên mặc định không ghi gì (dry-run).
/// Báo cáo ghi qua ILogger (Serilog Console) như mọi code khác, không dùng Console.WriteLine.
/// </summary>
public static class DevCommandExtensions
{
    /// <returns>true nếu <paramref name="args"/> là một lệnh bảo trì (đã xử lý xong, Program nên dừng); false = khởi động web bình thường.</returns>
    public static async Task<bool> TryRunDevCommandAsync(this WebApplication app, string[] args)
    {
        if (args.Length == 0 || args[0] != "reseed-foods") return false;

        var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("reseed-foods");
        if (!app.Environment.IsDevelopment()) return Fail(log, "reseed-foods chỉ chạy ở Development (ASPNETCORE_ENVIRONMENT=Development).");
        var apply = args.Contains("--apply");
        if (apply && !args.Contains("--backed-up"))
            return Fail(log, "--apply cần thêm --backed-up để xác nhận đã chạy scripts/food-catalog-backup.sql (hoặc backup DB) trước.");

        // Cùng công tắc với lúc khởi động: tắt thì alias/NameVi tiếng Việt bị dọn và không được seed lại
        CanonicalCatalogData.IncludeVietnamese = app.Configuration.GetValue("Seed:IncludeVietnamese", true);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cs = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        log.LogInformation("Database : {DataSource} / {Catalog}", cs.DataSource, cs.InitialCatalog);
        log.LogInformation("Chế độ   : {Mode}", apply ? "APPLY (ghi DB)" : "DRY-RUN (không ghi gì)");
        log.LogInformation("Tiếng Việt trong seed (Seed:IncludeVietnamese): {IncludeVietnamese}", CanonicalCatalogData.IncludeVietnamese);

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0) return Fail(log, $"DB còn {pending.Count} migration chưa áp ({string.Join(", ", pending)}). Chạy API một lần hoặc `dotnet ef database update` trước.");

        var r = await FoodCatalogReseeder.RunAsync(db, apply);
        log.LogInformation("Món catalog (seed/vn-fct/cond): {CatalogFoods}", r.CatalogFoods);
        log.LogInformation("  đổi tên sang tiếng Anh : {FoodsRenamed}; canonical đổi tên: {CanonicalsRenamed}", r.FoodsRenamed, r.CanonicalsRenamed);
        log.LogInformation("  sửa Language           : {LanguageFixed}", r.LanguageFixed);
        log.LogInformation("  đổi tên khẩu phần      : {ServingsRenamed}", r.ServingsRenamed);
        log.LogInformation("  icon đổi (mọi món hệ thống): {IconsChanged}; vẫn chưa có icon: {IconsStillNull}", r.IconsChanged, r.IconsStillNull);
        if (r.NullIconSamples.Count > 0) log.LogInformation("  món catalog chưa có icon: {NullIconSamples}", string.Join("; ", r.NullIconSamples));
        log.LogInformation("  alias Việt bị xoá      : {ViAliasesRemoved}; NameVi của canonical bị xoá: {CanonicalNameViCleared}", r.ViAliasesRemoved, r.CanonicalNameViCleared);
        log.LogInformation("Dữ liệu người dùng đang trỏ vào món catalog (KHÔNG bị xoá, món giữ nguyên Id):");
        foreach (var (table, count) in r.References) log.LogInformation("  {Table,-20}: {Count}", table, count);
        log.LogInformation(apply ? "Đã ghi xong (một transaction) và chạy lại seeder." : "Dry-run xong, chưa ghi gì. Backup rồi chạy lại với: -- reseed-foods --apply --backed-up");
        return true;
    }

    private static bool Fail(ILogger log, string message)
    {
        log.LogError("{Message}", message);
        Environment.ExitCode = 2;
        return true;
    }
}
