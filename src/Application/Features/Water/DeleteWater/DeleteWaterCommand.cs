namespace Application;

/// <summary>WP2: xoá một lần uống nước; tính lại FoodLog.TotalWater.</summary>
public sealed record DeleteWaterCommand(Guid Id) : IRequest<Unit>;
