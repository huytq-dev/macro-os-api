namespace Contract;

/// <summary>
/// Cấu hình JWT (mục "Jwt"): nguồn duy nhất cho cả phát token (JwtTokenService) lẫn kiểm tra token (JwtExtensions).
/// Đọc từ appsettings.json mục "Jwt".
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HmacSha256 cần khóa tối thiểu 256 bit.</summary>
    public const int MinSecretKeyBytes = 32;

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer    { get; init; } = string.Empty;
    public string Audience  { get; init; } = string.Empty;

    /// <summary>Thời hạn access token, tính bằng PHÚT.</summary>
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    /// <summary>Thời hạn refresh token, tính bằng NGÀY.</summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;

    /// <summary>Dừng khởi động khi thiếu hoặc sai cấu hình. Thông báo chỉ nêu tên biến, không in giá trị khóa.</summary>
    public void EnsureValid()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(SecretKey))
            problems.Add("thiếu JWT_KEY (Jwt:SecretKey)");
        else if (System.Text.Encoding.UTF8.GetByteCount(SecretKey) < MinSecretKeyBytes)
            problems.Add($"JWT_KEY (Jwt:SecretKey) quá ngắn, cần tối thiểu {MinSecretKeyBytes} byte UTF-8");
        if (string.IsNullOrWhiteSpace(Issuer))   problems.Add("thiếu JWT_ISSUER (Jwt:Issuer)");
        if (string.IsNullOrWhiteSpace(Audience)) problems.Add("thiếu JWT_AUDIENCE (Jwt:Audience)");
        if (AccessTokenExpirationMinutes <= 0)
            problems.Add("JWT_ACCESS_TOKEN_EXPIRATION (Jwt:AccessTokenExpirationMinutes, phút) phải lớn hơn 0");
        if (RefreshTokenExpirationDays <= 0)
            problems.Add("JWT_REFRESH_TOKEN_EXPIRATION (Jwt:RefreshTokenExpirationDays, ngày) phải lớn hơn 0");

        if (problems.Count > 0)
            throw new InvalidOperationException(
                $"Cấu hình JWT không hợp lệ: {string.Join("; ", problems)}. Hãy đặt trong src/API/.env hoặc biến môi trường.");
    }
}
