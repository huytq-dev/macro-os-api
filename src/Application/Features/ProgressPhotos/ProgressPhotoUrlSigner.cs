using System.Security.Cryptography;
using System.Text;

namespace Application;

/// <summary>
/// URL xem ảnh tiến độ do BE ký (HMAC-SHA256, gắn với ảnh + chủ ảnh + thời điểm hết hạn): &lt;img src&gt; không gửi được header Authorization,
/// nên quyền được chứng minh bằng chữ ký ngắn hạn thay vì URL công khai của kho ảnh. Hàm thuần.
/// </summary>
public static class ProgressPhotoUrlSigner
{
    public static string Sign(string secret, Guid photoId, Guid userId, long expUnix)
        => Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"progress-photo|{photoId:N}|{userId:N}|{expUnix}")));

    /// <summary>Chữ ký đúng (so sánh thời gian cố định) và chưa hết hạn.</summary>
    public static bool Verify(string secret, Guid photoId, Guid userId, long expUnix, string? signature, DateTimeOffset now)
        => expUnix > now.ToUnixTimeSeconds() && signature is { Length: 64 }
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(secret, photoId, userId, expUnix)), Encoding.ASCII.GetBytes(signature.ToLowerInvariant()));

    public static string Url(Guid photoId, long expUnix, string signature) => $"/api/progress-photos/{photoId}/content?exp={expUnix}&sig={signature}";
}
