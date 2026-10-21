namespace Application;

/// <summary>Ảnh tiến độ của một ngày (mặc định hôm nay) kèm URL xem do BE ký (hết hạn ngắn) và cờ đã đồng ý riêng tư.</summary>
public sealed record GetProgressPhotosQuery(DateOnly? Date) : IRequest<ProgressPhotosResponse>;
