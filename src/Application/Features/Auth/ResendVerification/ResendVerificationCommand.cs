namespace Application;

public sealed record ResendVerificationCommand(string Email) : IRequest<Unit>, ISensitiveRequest;
