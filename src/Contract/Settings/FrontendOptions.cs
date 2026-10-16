namespace Contract;

/// <summary>Địa chỉ FE để dựng link trong email (xác thực email...). Giá trị mặc định ở appsettings.json là địa chỉ deploy; máy dev ghi đè bằng dotnet user-secrets.</summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; init; } = "http://localhost:3000";
}
