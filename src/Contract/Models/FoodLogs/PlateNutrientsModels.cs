namespace Contract;

/// <summary>
/// Dinh dưỡng của plate (món chưa log) và của cả ngày nếu log plate này.
/// Cùng shape <see cref="NutrientAmountModel"/> với GET /foods/{id}/nutrients: đủ chất core (Amount null = chưa có dữ liệu), thêm carbs_net_g, target/floor/ceiling hiệu lực của <paramref name="Date"/>.
/// </summary>
/// <param name="Plate">Tổng các chất của riêng plate.</param>
/// <param name="Day">Tổng đã log của ngày (từ snapshot entry, gồm nước uống) cộng plate.</param>
public sealed record PlateNutrientsResponse(
    DateOnly Date,
    IReadOnlyList<NutrientAmountModel> Plate,
    IReadOnlyList<NutrientAmountModel> Day
);
