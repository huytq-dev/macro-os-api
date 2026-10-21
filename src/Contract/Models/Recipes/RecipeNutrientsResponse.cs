namespace Contract;

/// <summary>Dinh dưỡng của recipe đang soạn (chưa lưu): tổng và theo khẩu phần (tổng ÷ ServingsCount), mỗi bên đủ danh mục chất + % target của ngày Date.</summary>
public sealed record RecipeNutrientsResponse(
    DateOnly Date,
    decimal TotalGrams,
    int ServingsCount,
    RecipeMacros Total,
    RecipeMacros PerServing,
    IReadOnlyList<NutrientAmountModel> TotalNutrients,
    IReadOnlyList<NutrientAmountModel> PerServingNutrients
);
