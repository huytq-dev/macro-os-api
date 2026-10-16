namespace Contract;

public sealed class ApiResponse<T>
{
    public bool   IsSuccess  { get; init; }
    public int    StatusCode { get; init; }
    public string Message    { get; init; } = "";
    public T?     Data       { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = 200)
        => new() { IsSuccess = true, StatusCode = statusCode, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
        => new() { IsSuccess = false, StatusCode = statusCode, Message = message, Errors = errors };
}
