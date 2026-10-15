namespace Application;

public sealed record ConfirmEmailCommand(string Token) : IRequest<Unit>;
