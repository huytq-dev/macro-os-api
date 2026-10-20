namespace Contract;

/// <summary>
/// USDA FoodData Central. DEMO_KEY giới hạn ~30 request/giờ/IP — đủ cho dev; key riêng (miễn phí ở api.data.gov, 1000 request/giờ) KHÔNG ghi vào appsettings/repo:
/// đặt bằng <c>dotnet user-secrets set "Usda:ApiKey" "&lt;key&gt;" --project src/API</c> hoặc biến môi trường <c>Usda__ApiKey</c> (lệnh verify-usda bắt buộc có key riêng).
/// </summary>
public sealed class UsdaOptions
{
    public const string SectionName = "Usda";

    public string ApiKey { get; init; } = "DEMO_KEY";
}
