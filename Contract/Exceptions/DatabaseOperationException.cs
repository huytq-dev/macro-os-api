using Contract;

public class DatabaseOperationException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.InternalServerError, errors)
{
}