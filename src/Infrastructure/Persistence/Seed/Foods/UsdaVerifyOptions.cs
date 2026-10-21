namespace Infrastructure;

/// <param name="Only">seed | cond | vn-fct (null = cả ba).</param>
/// <param name="MaxRequests">Trần số request FDC mỗi lần chạy (FDC cho 1000 request/giờ/key); hết trần thì món còn lại ghi "not-checked", chạy lại sẽ tiếp tục (món đã xác minh được bỏ qua).</param>
/// <param name="Fdc">Chỉ định tay ExternalFoodId của món → fdcId (bỏ qua tìm kiếm; coi là khớp chắc).</param>
public sealed record UsdaVerifyOptions(bool Apply, string? Only, int MaxRequests, int DelayMs, int RetryDelayMs, IReadOnlyDictionary<string, int> Fdc);
