namespace Application;

public sealed record DeleteWeightEntryCommand(DateOnly LogDate) : IRequest<Unit>;
