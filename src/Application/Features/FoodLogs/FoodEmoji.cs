namespace Application;

/// <summary>Emoji đại diện món cho ô món trên timeline, suy từ nhóm (FoodCategory). Nhóm Other / không có món (recipe, quick add) → null, FE dùng emoji mặc định.</summary>
public static class FoodEmoji
{
    public static string? For(FoodCategory? category) => category switch
    {
        FoodCategory.Grains => "🍚",
        FoodCategory.Vegetables => "🥦",
        FoodCategory.Fruits => "🍎",
        FoodCategory.Dairy => "🥛",
        FoodCategory.Protein => "🍗",
        FoodCategory.Fats => "🥜",
        FoodCategory.Beverages => "🥤",
        FoodCategory.Snacks => "🍪",
        FoodCategory.FastFood => "🍔",
        FoodCategory.Supplements => "💊",
        _ => null,
    };
}
