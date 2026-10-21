namespace Application;

public sealed record DeleteBodyMetricCommand(Guid Id) : IRequest<Unit>;
