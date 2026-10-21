namespace Application;

/// <summary>Dữ liệu tra cứu cho cả lô dòng log: món (+serving), recipe, hồ sơ chất, mã chất. Nạp bằng số truy vấn cố định, không phụ thuộc số dòng.</summary>
public sealed class EntryLookup
{
    public required Dictionary<Guid, FoodItem> Foods { get; init; }
    public required Dictionary<Guid, Dictionary<string, NutrientValue>> FoodProfiles { get; init; }
    public required Dictionary<Guid, Recipe> Recipes { get; init; }
    public required Dictionary<Guid, Dictionary<string, NutrientValue>> RecipeProfiles { get; init; }  // trên 1 khẩu phần
    public required Dictionary<string, Guid> CodeIds { get; init; }
}
