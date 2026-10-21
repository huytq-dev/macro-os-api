namespace Contract;

/// <summary>Dịch vụ bên ngoài (vd Open Food Facts) lỗi hoặc quá thời gian → 502.</summary>
public sealed class ExternalServiceException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, (int)HttpStatusCode.BadGateway, errors);
