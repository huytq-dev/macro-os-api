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

/// <summary>Dịch vụ bên ngoài (vd Open Food Facts) lỗi hoặc quá thời gian → 502.</summary>
public sealed class ExternalServiceException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.BadGateway, errors);

/// <summary>Tính năng chưa được cấu hình (vd thiếu khoá AI) → 503.</summary>
public sealed class ServiceUnavailableException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.ServiceUnavailable, errors);

public class DatabaseOperationException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.InternalServerError, errors);
