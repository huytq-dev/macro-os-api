using Contract;

public sealed class UnauthorizedException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.Unauthorized, errors)
{
}