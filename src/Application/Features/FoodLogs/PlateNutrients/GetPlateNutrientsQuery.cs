namespace Application;

/// <summary>Xem trước dinh dưỡng của plate chưa log và của ngày nếu log plate này. Chỉ đọc, không ghi DB; Items rỗng hợp lệ.</summary>
public sealed record GetPlateNutrientsQuery(DateOnly? Date, List<PlateLine>? Items) : IRequest<PlateNutrientsResponse>;
