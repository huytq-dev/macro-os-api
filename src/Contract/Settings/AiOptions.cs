namespace Contract;

/// <summary>Phân tích bữa ăn bằng AI (mục "Ai" trong appsettings). ApiKey để trống = tính năng tắt, endpoint trả 503.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Khoá Anthropic API. Đặt bằng user-secrets / biến môi trường <c>Ai__ApiKey</c>, đừng commit giá trị thật.</summary>
    public string ApiKey { get; init; } = "";
    /// <summary>Model có vision; đổi sang bản rẻ hơn nếu cần giảm chi phí.</summary>
    public string Model { get; init; } = "claude-sonnet-5-5";
    /// <summary>Model rẻ, chỉ chữ, dùng ước lượng một nguyên liệu khi DB không có (gọi mỗi nguyên liệu không khớp).</summary>
    public string LookupModel { get; init; } = "claude-haiku-5-5";
    /// <summary>Quá thời gian này thì trả 502 để FE không treo.</summary>
    public int TimeoutSeconds { get; init; } = 45;
    public int MaxPhotos { get; init; } = 3;
    public int MaxPhotoBytes { get; init; } = 5 * 1024 * 1024;
    public int MaxDescriptionLength { get; init; } = 500;
}
