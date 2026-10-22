namespace Contract;

public sealed class BadRequestException(string message, IDictionary<string, string[]>? errors = null, object? details = null)
    : AppException(message, (int)HttpStatusCode.BadRequest, errors, details);
