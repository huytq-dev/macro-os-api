namespace Contract;

/// <summary>
/// Dinh dưỡng của plate (món chưa log) và của cả ngày nếu log plate này.
/// Cùng shape <see cref="NutrientAmountModel"/> với GET /foods/{id}/nutrients: đủ chất core (Amount null = chưa có dữ liệu), thêm carbs_net_g, target/floor/ceiling hiệu lực của <paramref name="Date"/>.
/// </summary>
/// <param name="Plate">Tổng các chất của RIÊNG plate (hoặc của riêng các entry đã chọn với POST entries/nutrients). Giá trị gốc, không bao giờ bị manual override đổi.</param>
/// <param name="Day">Tổng RAW đã log của ngày (từ snapshot entry, gồm nước uống) cộng plate = "foods + plate (raw)". KHÔNG áp manual override: ngày có override thì calories/macro ở đây KHÔNG phải lượng nạp hiệu lực. Vi chất chỉ từ món (không có vi chất giả).</param>
/// <param name="EffectiveDay">EFFECTIVE DAILY NUTRITION của <paramref name="Date"/> sau khi log plate: dinh dưỡng CẢ NGÀY (cùng shape <c>effective</c> của day log), KHÔNG PHẢI dinh dưỡng của plate hay của entry. Override thắng nên plate không đổi calories/macro hiệu lực, trường manual null giữ null (không lấy từ món, không thành 0); FoodLog* = subtotal RAW của món đã log + plate (cộng đúng một lần). Dùng cho calories/macro cards của ngày. null khi ngày chưa có dữ liệu và plate rỗng. Với POST entries/nutrients không có plate: EffectiveDay = hiệu lực hiện tại của ngày.</param>
public sealed record PlateNutrientsResponse(
    DateOnly Date,
    IReadOnlyList<NutrientAmountModel> Plate,
    IReadOnlyList<NutrientAmountModel> Day,
    EffectiveNutritionModel? EffectiveDay = null
);
