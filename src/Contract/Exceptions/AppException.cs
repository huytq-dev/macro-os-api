namespace Contract;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }
    /// <summary>Dữ liệu máy đọc được đi kèm lỗi (vào <c>data</c> của phản hồi); null với mọi lỗi thông thường.</summary>
    public object? Details { get; }
    protected AppException(string message, int statusCode = 500, IDictionary<string, string[]>? errors = null, object? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
        Details = details;
    }
}