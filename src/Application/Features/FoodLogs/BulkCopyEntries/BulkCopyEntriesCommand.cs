namespace Application;

/// <summary>
/// WP3 / Multi-Select: copy nhiều entry sang MỘT hoặc NHIỀU ngày (snapshot copy nguyên, không đọc lại món). Ngày đích = ToDates (≤ 62, bỏ trùng) cộng ToDate (body cũ).
/// ToTime thiếu → giữ giờ gốc từng entry; ToMealType thiếu → giữ bữa gốc (có ToTime mà không có ToMealType → bữa suy từ ToTime).
/// Một transaction, tổng ngày và expenditure tính lại một lần. Trả số entry đã tạo (= số entry nguồn × số ngày).
/// </summary>
public sealed record BulkCopyEntriesCommand(List<Guid> Ids, DateOnly? ToDate, TimeOnly? ToTime, MealType? ToMealType, List<DateOnly>? ToDates = null) : IRequest<int>
{
    public const int MaxDates = 62;
    public const int MaxCopies = 1000;

    /// <summary>Ngày đích đã bỏ trùng, theo thứ tự tăng dần.</summary>
    public List<DateOnly> TargetDates() => (ToDates ?? []).Concat(ToDate is { } d ? [d] : []).Distinct().Order().ToList();
}
