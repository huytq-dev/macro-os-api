namespace Contract;

/// <summary>Địa chỉ FE để dựng link trong email (xác thực email...). Đặt ở appsettings.json (Frontend:BaseUrl).</summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; init; } = "http://localhost:3000";
}
