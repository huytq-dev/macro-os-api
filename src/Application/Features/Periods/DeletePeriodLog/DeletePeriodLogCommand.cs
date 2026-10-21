namespace Application;

public sealed record DeletePeriodLogCommand(Guid Id) : IRequest<Unit>;
