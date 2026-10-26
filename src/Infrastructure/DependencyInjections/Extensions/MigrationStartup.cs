namespace Infrastructure;

/// <summary>
/// Áp migration khi khởi động, có công tắc <c>Database:AutoMigrate</c> (mặc định <c>true</c> = hành vi cũ: tự <c>MigrateAsync</c>).
/// <c>false</c> = KHÔNG tự áp: nếu còn migration chưa áp thì dừng khởi động với thông báo rõ (chạy script đã duyệt trước, xem Docs/Design/Ops/S29_Production_Readiness.md), nếu schema đã đủ thì chạy tiếp.
/// Dùng khi production phải áp S29 có giám sát (sao lưu → script → BE mới) thay vì để lần deploy đầu tiên tự áp.
/// </summary>
public static class MigrationStartup
{
    public static async Task ApplyAsync(ApplicationDbContext db, bool autoMigrate, ILogger logger, CancellationToken ct = default)
    {
        if (autoMigrate)
        {
            logger.LogInformation("Applying migrations...");
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("Migrations applied.");
            return;
        }

        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count > 0)
            throw new InvalidOperationException($"Database:AutoMigrate=false and {pending.Count} migration(s) are pending ({string.Join(", ", pending)}). Apply them with the approved script first, then start the app.");
        logger.LogInformation("Database:AutoMigrate=false; schema is up to date, no migration applied.");
    }
}
