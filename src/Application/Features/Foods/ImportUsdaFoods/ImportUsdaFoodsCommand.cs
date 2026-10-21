namespace Application;

/// <summary>
/// WP1: nhập món Foundation + SR Legacy từ USDA FoodData Central. Query (tìm theo từ khoá, lấy MaxItems món đầu) hoặc FdcIds (chỉ định).
/// Idempotent theo (ProviderCode = usda, ExternalFoodId = fdcId): món đã có thì bỏ qua. Mỗi lần chạy ghi một FoodImportRun. Mọi user đã đăng nhập đều chạy được.
/// </summary>
public sealed record ImportUsdaFoodsCommand(string? Query, List<int>? FdcIds, int MaxItems = 10) : IRequest<FoodImportRunModel>;
