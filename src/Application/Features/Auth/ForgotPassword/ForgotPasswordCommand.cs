namespace Application;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Unit>, ISensitiveRequest;
