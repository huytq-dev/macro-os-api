namespace Application;

public sealed record DeleteFoodLogEntryCommand(Guid Id) : IRequest<Unit>;
