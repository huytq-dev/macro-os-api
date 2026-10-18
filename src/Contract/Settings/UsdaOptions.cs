namespace Contract;

/// <summary>USDA FoodData Central. DEMO_KEY giới hạn ~30 request/giờ/IP — đủ cho dev; production dùng key riêng (api.data.gov).</summary>
public sealed class UsdaOptions
{
    public const string SectionName = "Usda";

    public string ApiKey { get; init; } = "DEMO_KEY";
}
