namespace Contract;

/// <param name="Range">today | week | month | quarter | year.</param>
/// <param name="From">Ngày đầu kỳ; kỳ kết thúc ở <c>To</c> = tham số date (mặc định hôm nay).</param>
/// <param name="LoggedDays">Số ngày đã log (có ≥ 1 món) trong kỳ; mẫu số của trung bình.</param>
/// <param name="Contributors">Chỉ có khi range = today.</param>
public sealed record NutritionOverviewResponse(
    string Range,
    DateOnly From,
    DateOnly To,
    int LoggedDays,
    IReadOnlyList<NutritionOverviewItem> Items,
    NutritionContributors? Contributors
);
