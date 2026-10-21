namespace Application;

public sealed record DeleteStepEntryCommand(DateOnly LogDate) : IRequest<Unit>;
