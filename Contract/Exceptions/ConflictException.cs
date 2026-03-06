using Contract;

public class ConflictException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.Conflict, errors)
{
}