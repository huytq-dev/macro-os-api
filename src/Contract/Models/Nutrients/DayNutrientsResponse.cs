namespace Contract;

/// <summary>Mọi nutrient của một ngày; thêm <c>carbs_net_g</c> (Net non-fiber = carbs − fiber, tính khi đọc, không có target).</summary>
public sealed record DayNutrientsResponse(DateOnly Date, IReadOnlyList<NutrientModel> Items);
