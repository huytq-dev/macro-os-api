using Microsoft.Data.SqlClient;

namespace Infrastructure;

/// <summary>
/// Lệnh bảo trì chạy từ dòng lệnh thay vì mở web, CHỈ ở Development, chạy TRƯỚC khi áp migration/seed của lần khởi động thường (Program.cs gọi trước ApplyMigrationsAsync):
/// <c>dotnet run --project src/API -- reseed-foods [--apply --backed-up]</c> và
/// <c>dotnet run --project src/API -- verify-usda [--apply --backed-up] [--only seed|cond|vn-fct] [--max-requests N] [--delay-ms N] [--fdc key=fdcId ...]</c>.
/// Mặc định dry-run (không ghi gì). Báo cáo ghi qua ILogger (Serilog Console) như mọi code khác, không dùng Console.WriteLine.
/// </summary>
public static class DevCommandExtensions
{
    private const string ReseedFoods = "reseed-foods", VerifyUsda = "verify-usda", ImportUsdaBulk = "import-usda-bulk", BackfillNutrients = "backfill-nutrients", FixUsdaServings = "fix-usda-servings", ReassignIcons = "reassign-icons";

    /// <returns>true nếu <paramref name="args"/> là một lệnh bảo trì (đã xử lý xong, Program nên dừng); false = khởi động web bình thường.</returns>
    public static async Task<bool> TryRunDevCommandAsync(this WebApplication app, string[] args)
    {
        if (args.Length == 0 || args[0] is not (ReseedFoods or VerifyUsda or ImportUsdaBulk or BackfillNutrients or FixUsdaServings or ReassignIcons)) return false;

        var command = args[0];
        var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(command);
        if (!app.Environment.IsDevelopment()) return Fail(log, $"{command} chỉ chạy ở Development (ASPNETCORE_ENVIRONMENT=Development).");
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

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0) return Fail(log, $"DB còn {pending.Count} migration chưa áp ({string.Join(", ", pending)}). Chạy API một lần hoặc `dotnet ef database update` trước.");

        return command switch
        {
            ReseedFoods => await ReseedAsync(db, log, apply),
            VerifyUsda => await VerifyUsdaAsync(app, scope, db, log, args, apply),
            BackfillNutrients => await BackfillNutrientsAsync(app, scope, db, log, args, apply),
            FixUsdaServings => await FixUsdaServingsAsync(scope, db, log, args, apply),
            ReassignIcons => await ReassignIconsAsync(scope, db, log, args, apply),
            _ => await ImportUsdaBulkAsync(scope, db, log, args, apply),
        };
    }

    // Gán lại icon món hệ thống bằng resolver hiện tại (không đụng món user tạo, không đụng UserFoodIcons); --dir (tuỳ chọn) để dùng nhóm FDC làm icon chung. Ghi thật dùng DbContext riêng.
    private static async Task<bool> ReassignIconsAsync(IServiceScope scope, ApplicationDbContext db, ILogger log, string[] args, bool apply)
    {
        var dir = Option(args, "--dir");
        if (dir is not null && !Directory.Exists(dir)) return Fail(log, "--dir phải là thư mục chứa file JSON FDC đã giải nén.");
        if (!int.TryParse(Option(args, "--batch") ?? "500", out var batch) || batch is < 10 or > 5000) return Fail(log, "--batch phải trong khoảng 10–5000 (mặc định 500 món/lô).");
        var report = Option(args, "--report") ?? Path.Combine(dir ?? Path.GetTempPath(), "icon-missing-concepts.csv");
        if (dir is null) log.LogInformation("Không có --dir: món USDA chỉ được gán theo tên (không có icon chung theo nhóm FDC). Thêm --dir BE/.usda-data để dùng nhóm.");

        var target = db;
        if (apply)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(db.Database.GetConnectionString(), sql => sql.MaxBatchSize(200).CommandTimeout(300))
                .AddInterceptors(scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;
            target = new ApplicationDbContext(options);
        }
        try
        {
            var r = await FoodIconReassigner.RunAsync(target, new FoodIconReassignOptions(apply, dir, batch), m => log.LogInformation("{Progress}", m));
            log.LogInformation("Quét {Foods} món hệ thống (catalog + USDA): đổi icon {Changed}; chưa có icon trước {WasNull} → sau {NowNull}; icon user đã chọn giữ nguyên ({Overrides} món có icon riêng của user)", r.Foods, r.Changed, r.WasNull, r.NowNull, r.UserOverridesKept);
            log.LogInformation("Top {Count} cặp đổi (cũ → mới):", r.Changes.Count);
            foreach (var c in r.Changes) log.LogInformation("  {Count,5}  {Old,-22} → {New,-22} vd: {Examples}", c.Count, c.Old, c.New, string.Join(" | ", c.Examples));
            log.LogInformation("Khái niệm THIẾU ICON THẬT: {Total} nhóm (none = không có icon; generic = chỉ có icon chung của nhóm). Top 40:", r.ConceptsTotal);
            foreach (var c in r.Concepts.Take(40)) log.LogInformation("  {Count,5}  {Kind,-7} {Concept,-32} vd: {Examples}", c.Count, c.Kind, c.Concept, string.Join(" | ", c.Examples.Take(2)));
            FoodIconReassigner.WriteConceptsCsv(report, r.Concepts);
            log.LogInformation("CSV khái niệm thiếu icon (cho người vẽ/AI): {Report}", Path.GetFullPath(report));
            log.LogInformation(apply ? "Đã đổi icon {Changed} món trong {Elapsed:mm\\:ss}." : "Dry-run xong ({Elapsed:mm\\:ss}), chưa ghi gì. Backup rồi chạy lại với: -- reassign-icons --dir BE/.usda-data --apply --backed-up", apply ? r.Changed : r.Elapsed, r.Elapsed);
            return true;
        }
        finally
        {
            if (!ReferenceEquals(target, db)) await target.DisposeAsync();
        }
    }

    // Sửa khẩu phần tên toàn số ("1 90000") của món USDA đã nhập; không cần file FDC (FDC không có mô tả hợp lệ cho các khẩu phần này). Ghi thật dùng DbContext riêng như import-usda-bulk.
    private static async Task<bool> FixUsdaServingsAsync(IServiceScope scope, ApplicationDbContext db, ILogger log, string[] args, bool apply)
    {
        if (!int.TryParse(Option(args, "--batch") ?? "300", out var batch) || batch is < 10 or > 5000) return Fail(log, "--batch phải trong khoảng 10–5000 (mặc định 300 món/lô).");
        var target = db;
        if (apply)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(db.Database.GetConnectionString(), sql => sql.MaxBatchSize(200).CommandTimeout(300))
                .AddInterceptors(scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;
            target = new ApplicationDbContext(options);
        }
        try
        {
            var r = await UsdaServingFixer.RunAsync(target, new UsdaServingFixOptions(apply, batch), m => log.LogInformation("{Progress}", m));
            log.LogInformation("Quét {Foods} món USDA: {Affected} món có khẩu phần tên sai (toàn số như \"1 90000\")", r.UsdaFoods, r.FoodsAffected);
            log.LogInformation("  đổi tên khẩu phần toàn số (\"1 90000\") thành \"1 serving\" (giữ Id + gram, trùng và đang được tham chiếu thì theo gram): {Renamed}; xoá {Deleted} khẩu phần trùng tên chưa ai dùng; chọn lại mặc định {Defaults} món (ưu tiên cup/khẩu phần đếm được, không thì \"100 g\"; tạo mới \"100 g\" cho {Created} món)", r.Renamed, r.Deleted, r.DefaultsReset, r.HundredGramCreated);
            log.LogInformation("  nhãn viết tắt \"RACC\" (Foundation) đổi thành \"serving\": {Racc} khẩu phần; bỏ mã món ghép vào nhãn (\"0.5 cup From 19211\"): {Code} khẩu phần", r.RaccRenamed, r.CodeStripped);
            foreach (var s in r.Samples) log.LogInformation("    vd: {Sample}", s);
            log.LogInformation(apply ? "Đã sửa xong trong {Elapsed:mm\\:ss}." : "Dry-run xong ({Elapsed:mm\\:ss}), chưa ghi gì. Backup rồi chạy lại với: -- fix-usda-servings --apply --backed-up", r.Elapsed);
            return true;
        }
        finally
        {
            if (!ReferenceEquals(target, db)) await target.DisposeAsync();
        }
    }

    // Bổ sung chất còn thiếu cho món đã có từ file FDC local (món catalog thiếu file thì gọi API nếu có khoá riêng; --no-api tắt hẳn). Ghi thật dùng DbContext riêng như import-usda-bulk.
    private static async Task<bool> BackfillNutrientsAsync(WebApplication app, IServiceScope scope, ApplicationDbContext db, ILogger log, string[] args, bool apply)
    {
        var dir = Option(args, "--dir");
        if (dir is null || !Directory.Exists(dir)) return Fail(log, "--dir <thư mục> là bắt buộc và phải tồn tại: thư mục chứa file JSON FDC đã giải nén (xem scripts/download-usda.ps1).");
        if (!int.TryParse(Option(args, "--batch") ?? "300", out var batch) || batch is < 10 or > 5000) return Fail(log, "--batch phải trong khoảng 10–5000 (mặc định 300 món/lô).");
        if (!int.TryParse(Option(args, "--max-requests") ?? "50", out var maxRequests) || maxRequests < 0) return Fail(log, "--max-requests phải là số nguyên ≥ 0.");
        var report = Option(args, "--report") ?? Path.Combine(dir, "usda-backfill-no-fdc.csv");

        // API chỉ khi có khoá riêng (DEMO_KEY ~30 request/giờ không đủ) và không có --no-api
        var key = app.Configuration["Usda:ApiKey"];
        var useApi = !args.Contains("--no-api") && maxRequests > 0 && !string.IsNullOrWhiteSpace(key) && key != "DEMO_KEY";
        var usda = useApi ? scope.ServiceProvider.GetRequiredService<IUsdaClient>() : null;
        log.LogInformation("FDC API cho món catalog thiếu trong file local: {Api}", useApi ? $"bật (tối đa {maxRequests} request)" : "tắt");

        var target = db;
        if (apply)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(db.Database.GetConnectionString(), sql => sql.MaxBatchSize(200).CommandTimeout(300))
                .AddInterceptors(scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;
            target = new ApplicationDbContext(options);
        }

        try
        {
            var r = await UsdaNutrientBackfill.RunAsync(target, new UsdaBackfillOptions(apply, dir, batch, maxRequests, 300), usda, TimeProvider.System, m => log.LogInformation("{Progress}", m));
            log.LogInformation("Đọc file FDC: {ByDataset}", string.Join(", ", r.ReadByDataset.Select(kv => $"{kv.Key} {kv.Value}")));
            foreach (var (name, g) in new[] { ("(a) món USDA nhập về", r.UsdaFoods), ("(b) món catalog có nguồn USDA", r.CatalogFoods) })
            {
                log.LogInformation("{Group}: {Foods} món; thêm chất cho {ToUpdate} món ({Rows} dòng); không lấy được dữ liệu FDC: {NotFound}", name, g.Foods, g.FoodsToUpdate, g.Rows, g.NotFound);
                if (g.NotFoundSamples.Count > 0) log.LogInformation("    vd: {Samples}", string.Join(" | ", g.NotFoundSamples));
            }
            log.LogInformation("Món USDA nhập trước đó có macro (P/C/F) = 0 dù FDC có số (Foundation: fat 298, carbs 205.2…): {Fixes} món{Samples}", r.MacroFixes, r.MacroFixSamples is { Count: > 0 } s ? " — vd " + string.Join(" | ", s) : "");
            log.LogInformation("FDC API: {Requests} request, {Rows} dòng chất lấy từ API{Error}", r.ApiRequests, r.RowsFromApi, r.ApiError is null ? "" : $" — dừng: {r.ApiError}");
            log.LogInformation("(c) món catalog KHÔNG có FDC (chỉ liệt kê, không ghi số): {Count}", r.NoFdc.Count);
            foreach (var n in r.NoFdc.Take(15)) log.LogInformation("    {Provider,-7} {Name,-40} {Nutrients,2} chất   gợi ý: {Suggestions}", n.Provider, n.Name, n.Nutrients, string.Join(" | ", n.Suggestions));
            UsdaNutrientBackfill.WriteNoFdcCsv(report, r.NoFdc);
            log.LogInformation("CSV món không có FDC kèm gợi ý: {Report}", Path.GetFullPath(report));
            log.LogInformation("Độ phủ ≥ {Min} chất — catalog (seed/vn-fct/cond): {CatNow}/{CatFoods} ({CatNowPct:0.0}%) → {CatAfter} ({CatAfterPct:0.0}%); món USDA nhập về: {UsdaNow}/{UsdaFoods} ({UsdaNowPct:0.0}%) → {UsdaAfter} ({UsdaAfterPct:0.0}%)",
                UsdaNutrientBackfill.MinNutrientsForCoverage, r.CatalogCoverage.Now, r.CatalogCoverage.Foods, r.CatalogCoverage.NowPercent, r.CatalogCoverage.After, r.CatalogCoverage.AfterPercent,
                r.UsdaCoverage.Now, r.UsdaCoverage.Foods, r.UsdaCoverage.NowPercent, r.UsdaCoverage.After, r.UsdaCoverage.AfterPercent);
            if (apply) log.LogInformation("Đã thêm {Inserted} dòng chất trong {Elapsed:mm\\:ss}.", r.RowsInserted, r.Elapsed);
            else log.LogInformation("Dry-run xong ({Elapsed:mm\\:ss}), chưa ghi gì. Backup rồi chạy lại với: -- backfill-nutrients --dir \"{Dir}\" --apply --backed-up", r.Elapsed, dir);
            log.LogInformation("Lưu ý: món đã log trước đây giữ nguyên snapshot cũ (không đổi); chỉ lần log MỚI mới có đủ chất.");
            return true;
        }
        finally
        {
            if (!ReferenceEquals(target, db)) await target.DisposeAsync();
        }
    }

    // Nhập hàng loạt từ file tải về của FDC (không gọi API nên không vướng hạn mức). Ghi thật dùng DbContext riêng với lô lệnh lớn hơn cho SQL Server từ xa (vẫn qua interceptor audit).
    private static async Task<bool> ImportUsdaBulkAsync(IServiceScope scope, ApplicationDbContext db, ILogger log, string[] args, bool apply)
    {
        var dir = Option(args, "--dir");
        if (dir is null || !Directory.Exists(dir)) return Fail(log, "--dir <thư mục> là bắt buộc và phải tồn tại: thư mục chứa file JSON FDC đã giải nén (xem scripts/download-usda.ps1).");
        var only = Option(args, "--only");
        if (only is not null && only is not ("foundation" or "sr" or "fndds")) return Fail(log, "--only phải là foundation, sr hoặc fndds.");
        int? max = null;
        if (Option(args, "--max") is { } maxText)
        {
            if (!int.TryParse(maxText, out var parsed) || parsed < 1) return Fail(log, "--max phải là số nguyên ≥ 1.");
            max = parsed;
        }
        if (!int.TryParse(Option(args, "--batch") ?? "500", out var batch) || batch is < 50 or > 5000) return Fail(log, "--batch phải trong khoảng 50–5000 (mặc định 500 món/lô).");
        var report = Option(args, "--report") ?? Path.Combine(dir, "usda-bulk-no-icon.csv");

        var target = db;
        if (apply)
        {
            var connection = db.Database.GetConnectionString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connection, sql => sql.MaxBatchSize(200).CommandTimeout(300))
                .AddInterceptors(scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;
            target = new ApplicationDbContext(options);
        }

        try
        {
            var r = await UsdaBulkImporter.RunAsync(target, new UsdaBulkOptions(apply, dir, only, max, batch, args.Contains("--all-nutrients")), TimeProvider.System, m => log.LogInformation("{Progress}", m));
            log.LogInformation("Đọc {Read} món: {ByDataset}", r.Read, string.Join(", ", r.ReadByDataset.Select(kv => $"{kv.Key} {kv.Value}")));
            if (r.IgnoredFiles.Count > 0) log.LogInformation("Bỏ qua file không phải Foundation/SR Legacy/FNDDS: {Files}", string.Join(", ", r.IgnoredFiles));
            foreach (var (reason, count) in r.Skipped.OrderByDescending(kv => kv.Value))
                log.LogInformation("  loại {Reason,-20} {Count,6}   vd: {Samples}", reason, count, string.Join(" | ", r.SkipSamples.GetValueOrDefault(reason) ?? []));
            log.LogInformation("Sẽ thêm {ToAdd} món ({ByDataset}), {Servings} khẩu phần, {Nutrients} dòng chất, {Aliases} alias tên gốc; có icon {WithIcon}/{ToAdd} ({Percent:0.0}%)",
                r.ToAdd, string.Join(", ", r.ToAddByDataset.Select(kv => $"{kv.Key} {kv.Value}")), r.Servings, r.Nutrients, r.Aliases, r.WithIcon, r.ToAdd, r.IconPercent);
            foreach (var c in r.NoIcon.Take(15)) log.LogInformation("  chưa có icon: {Category,-45} {Count,5}   vd: {Examples}", c.Category, c.Count, string.Join(" | ", c.Examples));
            UsdaBulkImporter.WriteNoIconCsv(report, r.NoIcon);
            log.LogInformation("CSV món chưa có icon theo nhóm USDA: {Report} ({Categories} nhóm)", Path.GetFullPath(report), r.NoIcon.Count);
            if (apply) log.LogInformation("Đã nhập {Inserted} món trong {Elapsed:mm\\:ss}.", r.Inserted, r.Elapsed);
            else log.LogInformation("Dry-run xong ({Elapsed:mm\\:ss}), chưa ghi gì. Backup rồi chạy lại với: -- import-usda-bulk --dir \"{Dir}\" --apply --backed-up", r.Elapsed, dir);
            return true;
        }
        finally
        {
            if (!ReferenceEquals(target, db)) await target.DisposeAsync();
        }
    }

    private static async Task<bool> ReseedAsync(ApplicationDbContext db, ILogger log, bool apply)
    {
        log.LogInformation("Tiếng Việt trong seed (Seed:IncludeVietnamese): {IncludeVietnamese}", CanonicalCatalogData.IncludeVietnamese);

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

    private static async Task<bool> VerifyUsdaAsync(WebApplication app, IServiceScope scope, ApplicationDbContext db, ILogger log, string[] args, bool apply)
    {
        // Khoá FDC đọc từ user-secrets / biến môi trường (Usda__ApiKey), KHÔNG nằm trong appsettings; DEMO_KEY chỉ ~30 request/giờ nên không đủ cho cả catalog
        var key = app.Configuration["Usda:ApiKey"];
        if (string.IsNullOrWhiteSpace(key) || key == "DEMO_KEY")
            return Fail(log, "Thiếu khoá USDA riêng. Lấy miễn phí ở https://api.data.gov/signup/ rồi đặt: dotnet user-secrets set \"Usda:ApiKey\" \"<key>\" --project src/API  (hoặc biến môi trường Usda__ApiKey).");

        var only = Option(args, "--only");
        if (only is not null && only is not (FoodData.SeedProvider or CanonicalCatalogData.CondimentProvider or CanonicalCatalogData.VnProvider))
            return Fail(log, "--only phải là seed, cond hoặc vn-fct.");
        if (!int.TryParse(Option(args, "--max-requests") ?? "900", out var maxRequests) || maxRequests < 2) return Fail(log, "--max-requests phải là số nguyên ≥ 2.");
        if (!int.TryParse(Option(args, "--delay-ms") ?? "300", out var delayMs) || delayMs < 0) return Fail(log, "--delay-ms phải là số nguyên ≥ 0.");

        var fdc = new Dictionary<string, int>();
        foreach (var pair in Options(args, "--fdc"))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2 || parts[0].Length == 0 || !int.TryParse(parts[1], out var id) || id <= 0) return Fail(log, $"--fdc cần dạng key=fdcId (nhận '{pair}').");
            fdc[parts[0]] = id;
        }

        var usda = scope.ServiceProvider.GetRequiredService<IUsdaClient>();
        log.LogInformation("USDA FDC: tối đa {MaxRequests} request (FDC giới hạn 1000 request/giờ/key), giãn {DelayMs} ms giữa các request", maxRequests, delayMs);
        var report = await UsdaCatalogVerifier.RunAsync(db, usda, new UsdaVerifyOptions(apply, only, maxRequests, delayMs, 30_000, fdc), TimeProvider.System);

        // Dòng chữ dựng sẵn (không dùng mẫu log nhiều tham số): trước đây REVIEW/WEAK lệch số tham số so với mẫu nên bị logger bỏ mất dòng
        foreach (var line in UsdaVerifyReportFormatter.Lines(report, apply)) log.LogInformation("{Line}", line);
        if (Option(args, "--report") is { } csv)
        {
            UsdaVerifyReportFormatter.WriteCsv(csv, report);
            log.LogInformation("CSV báo cáo (mỗi ứng viên một dòng): {Report}", Path.GetFullPath(csv));
        }
        return true;
    }

    private static string? Option(string[] args, string name) => Options(args, name).FirstOrDefault();

    private static IEnumerable<string> Options(string[] args, string name)
        => args.Select((a, i) => (a, i)).Where(x => x.a == name && x.i + 1 < args.Length).Select(x => args[x.i + 1]);

    private static bool Fail(ILogger log, string message)
    {
        log.LogError("{Message}", message);
        Environment.ExitCode = 2;
        return true;
    }
}
