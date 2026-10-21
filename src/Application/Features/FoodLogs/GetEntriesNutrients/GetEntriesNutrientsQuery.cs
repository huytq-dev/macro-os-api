namespace Application;

/// <summary>
/// Multi-Select / View: dinh dưỡng của các entry đã chọn (đọc từ snapshot lúc log: macro/fiber/sodium từ cột entry, còn lại từ FoodLogEntryNutrient).
/// Cùng shape với plate/nutrients: <c>Plate</c> = tổng các entry đã chọn, <c>Day</c> = tổng đã log của <c>Date</c> (đã gồm các entry đó, nên không cộng thêm).
/// <c>Date</c> thiếu → ngày có nhiều entry được chọn nhất (hoà thì ngày sớm hơn); mục tiêu % tính theo ngày này. Id không phải của mình bị bỏ qua; không có id nào là của mình → 404.
/// </summary>
public sealed record GetEntriesNutrientsQuery(List<Guid> Ids, DateOnly? Date = null) : IRequest<PlateNutrientsResponse>;
