namespace Application;

/// <summary>WP2: ghi một lần uống nước (LogDate null = hôm nay; Time thiếu → giờ hiện tại nếu là hôm nay, ngược lại 12:00). Cập nhật FoodLog.TotalWater.</summary>
public sealed record AddWaterCommand(DateOnly? LogDate, decimal AmountMl, TimeOnly? Time, string? Source) : IRequest<Guid>;
