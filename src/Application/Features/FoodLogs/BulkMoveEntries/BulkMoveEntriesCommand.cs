namespace Application;

/// <summary>
/// WP3: chuyển nhiều entry sang ngày/giờ/bữa khác (giữ nguyên Id và snapshot). Cần ít nhất một trong ToDate, ToTime, ToMealType.
/// ToTime mà không có ToMealType → bữa suy từ ToTime. Trả số entry đã chuyển; tổng của mọi ngày bị ảnh hưởng và expenditure tính lại một lần.
/// </summary>
public sealed record BulkMoveEntriesCommand(List<Guid> Ids, DateOnly? ToDate, TimeOnly? ToTime, MealType? ToMealType) : IRequest<int>;
