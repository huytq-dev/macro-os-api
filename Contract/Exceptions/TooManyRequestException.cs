using Contract;

public sealed class TooManyRequestException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.TooManyRequests, errors)
{
}