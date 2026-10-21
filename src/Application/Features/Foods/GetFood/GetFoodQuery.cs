namespace Application;

public sealed record GetFoodQuery(Guid Id) : IRequest<FoodItemResponse>;
