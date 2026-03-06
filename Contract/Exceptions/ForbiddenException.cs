using Contract;

public sealed class ForbiddenException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.Forbidden, errors)
{
}