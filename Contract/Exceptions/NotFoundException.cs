using Contract;

public sealed class NotFoundException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.NotFound, errors)
{
}