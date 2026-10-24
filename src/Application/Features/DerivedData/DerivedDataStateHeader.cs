namespace Application;

/// <summary>
/// Header <c>X-Derived-Data-State</c> (Ready | Updating | Failed) trên phản hồi của mọi lệnh ghi thành công: cho FE biết ngay dữ liệu dẫn xuất đã mới hay còn job nền, khỏi gọi thêm
/// <c>GET /api/derived-data/status</c>. Giá trị = đúng trạng thái của endpoint đó (<see cref="DerivedDataStatusResponse"/>). Hàm thuần quyết định phản hồi nào được gắn header.
/// </summary>
public static class DerivedDataStateHeader
{
    public const string Name = "X-Derived-Data-State";

    /// <summary>
    /// Lệnh ghi (không phải GET/HEAD/OPTIONS) đã đăng nhập, thành công (2xx), không thuộc <c>/api/auth</c> (không có dữ liệu user) và không phải <c>/api/derived-data</c> (phản hồi đã mang trạng thái).
    /// </summary>
    public static bool ShouldAttach(string method, string path, int statusCode, bool authenticated)
    {
        if (!authenticated || statusCode is < 200 or >= 300) return false;
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase)) return false;
        return !IsUnder(path, "/api/auth") && !IsUnder(path, "/api/derived-data");
    }

    private static bool IsUnder(string path, string prefix)
        => path.Equals(prefix, StringComparison.OrdinalIgnoreCase) || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
}
