namespace Contract;

public sealed class BadRequestException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.BadRequest, errors);

public class ConflictException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.Conflict, errors);

public sealed class ForbiddenException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.Forbidden, errors);

public sealed class NotFoundException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.NotFound, errors);

public sealed class TooManyRequestException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.TooManyRequests, errors);

public sealed class UnauthorizedException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.Unauthorized, errors);

public class DatabaseOperationException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.InternalServerError, errors);
