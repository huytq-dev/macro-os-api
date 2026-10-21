namespace Contract;

/// <summary>Top 3 món đóng góp nhiều nhất trong ngày cho mỗi macro (gộp theo tên món).</summary>
public sealed record NutritionContributors(
    IReadOnlyList<NutritionContributor> Calories,
    IReadOnlyList<NutritionContributor> Protein,
    IReadOnlyList<NutritionContributor> Fat,
    IReadOnlyList<NutritionContributor> Carbs
);
