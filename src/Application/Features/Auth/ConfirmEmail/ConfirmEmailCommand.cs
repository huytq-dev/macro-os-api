namespace Application;

public sealed record ConfirmEmailCommand(
    Guid UserId,
    string Token
) : IRequest<Unit>;
