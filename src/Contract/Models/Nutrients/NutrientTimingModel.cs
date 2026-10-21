namespace Contract;

/// <summary>Lượng nutrient ăn trong khung giờ <paramref name="Hour"/> (0–23, giờ local của user).</summary>
public sealed record NutrientTimingModel(int Hour, decimal Amount);
