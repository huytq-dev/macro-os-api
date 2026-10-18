namespace Contract;

/// <param name="Status">Running | Succeeded | Partial | Failed.</param>
/// <param name="Imported">Số món mới tạo.</param>
/// <param name="Skipped">Số món đã có từ trước (idempotent) hoặc bị bỏ qua vì thiếu dữ liệu năng lượng.</param>
public sealed record FoodImportRunModel(
    Guid Id,
    string Provider,
    string? Query,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int Imported,
    int Skipped,
    string? Error
);
