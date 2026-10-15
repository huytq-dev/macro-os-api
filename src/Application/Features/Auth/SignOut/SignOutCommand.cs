namespace Application;

public sealed record SignOutCommand(Guid UserId) : IRequest<Unit>;
