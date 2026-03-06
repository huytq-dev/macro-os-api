using Contract;

public sealed class BadRequestException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.BadRequest, errors)
{
}