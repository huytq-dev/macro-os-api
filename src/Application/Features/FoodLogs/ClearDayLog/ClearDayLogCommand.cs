namespace Application;

/// <summary>UC-D07: xoá cả ngày (hard delete FoodLog, cascade entries) → thuật toán coi ngày này là chưa log.</summary>
public sealed record ClearDayLogCommand(DateOnly Date) : IRequest<Unit>;
