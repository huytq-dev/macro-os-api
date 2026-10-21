namespace Application;

/// <summary>
/// F10: mọi chất của một món theo khẩu phần, kèm floor/target/ceiling hiệu lực và % so target của ngày <c>Date</c> (mặc định hôm nay).
/// ServingId thiếu → khẩu phần mặc định; Quantity = số khẩu phần (mặc định 1).
/// </summary>
public sealed record GetFoodNutrientsQuery(Guid Id, Guid? ServingId, decimal Quantity = 1, DateOnly? Date = null) : IRequest<FoodNutrientsResponse>;
