namespace Application;

public sealed record DeleteCustomFoodCommand(Guid Id) : IRequest<Unit>;
