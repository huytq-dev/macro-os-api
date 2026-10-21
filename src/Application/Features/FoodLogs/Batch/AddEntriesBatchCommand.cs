namespace Application;

/// <summary>
/// WP3: log cả plate trong một giao dịch (thay vòng lặp tuần tự của FE). IdempotencyKey (≤ 100 ký tự, FE tạo mỗi lần bấm Log) chống nhân đôi khi retry:
/// key đã dùng → trả lại entry của lần đầu, không ghi gì. Tổng ngày và expenditure tính lại đúng một lần cho cả lô.
/// </summary>
public sealed record AddEntriesBatchCommand(
    string IdempotencyKey,
    DateOnly? LogDate,
    MealType? MealType,
    TimeOnly? EatenAt,
    string? Source,
    List<BatchEntryItem> Items
) : IRequest<BatchEntriesResponse>;
