namespace Contract;

/// <summary>Tính năng chưa được cấu hình (vd thiếu khoá AI) → 503.</summary>
public sealed class ServiceUnavailableException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.ServiceUnavailable, errors);
